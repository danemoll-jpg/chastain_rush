using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Supplies pedals and steering to the same motor as the player. No speed multipliers or positional driving.
    //
    // Chastain Rush, Round 15 (item 6): Racer's RoadDriver, trimmed to racing. Kept as Racer has it: pure-pursuit steering on
    // the race road with a speed-scaled look-ahead, the lane and the pass to the other lane when a slower car is ahead (only
    // when that lane is clear), speed planned from the bends (corner grip x cornerUse), hills and crests over the next 100 m
    // with braking judgment (brakeUse), the bounded sphere-cast obstacle sensor (ignores its own body and surfaces facing up),
    // DriverVariation (wide lines, early braking), ShortcutStrategy (when to take a shortcut), stall back-off and recovery
    // when stuck or off the route (no progress for 8 s, or 5 s when slow or on its side), the finish run-off and parking.
    // Racer's difficulty default is used (Normal, index 1).
    // Left behind: ambient traffic and the highway, Racer's map-specific run-ups (ForestLayout, MountainFlights, the Backyard
    // course), split-screen, campaign. Adapted: the race is RaceController here (its rules and gates); a shortcut is an
    // AiShortcut (Racer's WoodlandRoute: a path, where it leaves and rejoins the road, a speed, the vehicles it is validated
    // for); on a shortcut the car follows its line as this game's shortcut check does (heading plus a cross-track term), at the
    // shortcut's entry speed held through its run-up and jump (Racer's aiEntrySpeed rule), and the obstacle sensor ignores
    // the static world there (ramps, the stairs, the dirt pile are part of the line). Recovery places the car on the line or the
    // lap, clear of everything (as CarRecovery does for the player), never in water.
    public sealed class RoadDriver : MonoBehaviour
    {
        public static int Difficulty = 1;   // Chastain Rush: Racer's RaceDirector.difficulty default ("Normal")
        public RaceController Race;
        public ArcadeVehicle Car;
        [System.NonSerialized] public RaceEntrant Racer;
        public int Direction => 1;
        public float TargetSpeed { get; private set; }
        public DriverVariation Variation { get; } = new();
        public ShortcutStrategy Shortcuts { get; } = new();
        public AiShortcut ForcedShortcut;   // checks only: always take this one when it comes
        public static bool CheckTakeAll;    // Chastain Rush, Round 28 (checks only): take every open shortcut and every lap option when it comes
        public bool? ForceSloppy;           // checks only: a sloppy try (true) or a clean one (false) on a shortcut with 'slip'
        public bool Sloppy { get; private set; }   // Chastain Rush, Round 20 follow-ups: this try at the planned shortcut is a sloppy one
        public AiShortcut RunIn;            // Chastain Rush: from the grid on the entrance lane to the lap (set by RaceController)

        public int RecoveryCount { get; private set; }
        public float StalledSeconds => stalled;
        public float LongestStall { get; private set; }   // checks: the longest time this driver went with no progress
        public float LastThrottle { get; private set; }
        public float LastBrake { get; private set; }
        public string LastObstacle { get; private set; }
        public float BrakingSeconds { get; private set; }
        public AiShortcut OnShortcut => progressBranch;
        public int ShortcutsTaken { get; private set; }
        // Chastain Rush, Round 55 (E, Dan: "the AI drivers need to be smart enough to drive around cars ... I see them stuck to bumpers"): for the checks,
        // the passes made (out into the other lane past a slower car and back) and the time spent close behind a car at under 10 m/s
        public int Passes { get; private set; }
        public float BehindSeconds { get; private set; }
        bool passing; float passLaneNow = float.NaN;
        public static bool PassFix = true;   // checks: false (-r55PassOff) drives as before Round 55, for the before / after numbers

        public RaceRoad DriveRoad => Race.AiRoad;
        public float Lane => lane;   // Chastain Rush, Round 21: DriverVariation's curb clip goes to this side
        // Chastain Rush, Round 21: a mistake at the jump being approached (DriverVariation.RollJump): its ai_zone, the kind (4 short,
        // 2 off line), the side, and the lap it was rolled on (once per jump per lap)
        RouteAiZoneJson jumpZone; int jumpKind, jumpLap=-1; float jumpSide; string jumpRolled;
        public int SlipsTaken { get; private set; }
        float wadeTime;   // checks: sloppy tries at a shortcut (R1)
        bool finishParked;
        AiShortcut plannedBranch, progressBranch;
        float branchLast = -1f;   // Chastain Rush, Round 40
        float branchBest, branchStuck;
        float routeStuck, progressStation;
        bool progressSample;
        float rejoinStuck, rejoinBestDistance;
        Vector3 rejoinTarget;
        bool trackingRejoin, airborne;
        static readonly float[] cornerUse={.46f,.76f,.90f}, brakeUse={.55f,.88f,.98f}, speedUse={.91f,1f,1f}, hillTargets={26,32,35};
        float pace, stalled, lane, finishRunoff, nextRecovery;
        float departureLane,recoveryDepartureStation,recoveryDepartureUntil;
        readonly RaycastHit[] hits = new RaycastHit[24];
        bool obstacleVehicle;   // Chastain Rush: what stopped it last was another vehicle
        static readonly Collider[] overlap = new Collider[16];

        // Chastain Rush, Round 18: on a narrow trail (Noonday Creek) the lanes are the trail's own (RouteJson.ai_lane_m, a pass
        // lane ai_pass_lane_m), the car follows the line closely (the shortcut steering: the line winds between trunks), the
        // obstacle sensor counts only vehicles (the trunks beside the line are not in the way), and over each jump it holds the
        // route's set speed (ai_zones)
        bool trail; float laneSide = 1.7f, passLane = 2.3f, laneGap = 2.5f; AiShortcut trailLine;
        public void Initialize(RaceController race, ArcadeVehicle car, RaceEntrant racer, float variation, int index)
        {
            Race = race; Car = car; Racer = racer;
            pace = variation;
            var rd = race.RouteData; trail = rd != null && rd.trail;
            if (trail) { laneSide = rd.ai_lane_m > 0 ? rd.ai_lane_m : .55f; passLane = rd.ai_pass_lane_m > 0 ? rd.ai_pass_lane_m : .85f; laneGap = 1f; trailLine = new AiShortcut { Id = "trail", Name = "trail", Line = race.AiRoad, halfWidth = race.AiRoad.halfWidth }; }
            lane = index % 2 == 0 ? laneSide : -laneSide;
            Variation.Initialize(DriverVariation.Seed, index+Mathf.RoundToInt(variation*1000));
            Shortcuts.Initialize(System.Guid.NewGuid().GetHashCode());
            jumpZone = null; jumpRolled = null; jumpLap = -1; SlipsTaken = 0;   // Chastain Rush, Round 21
            finishParked = false; finishRunoff = 0; stalled = 0; plannedBranch = progressBranch = null; progressSample = false; trackingRejoin = false;
        }

        void FixedUpdate()
        {
            if (!Race || !Car || Racer == null)
                return;
            Car.enabled = false;
            if (Racer.Estimated) { Car.Body.isKinematic = true; return; }
            if (!Race.AiRunning)
            {
                Car.Body.isKinematic = true;
                return;
            }

            bool finished = Racer.Finished;
            if(finished && finishParked) { Car.Body.isKinematic=true; return; }
            Car.Body.isKinematic = false;
            // Chastain Rush, Round 28: a route with a parking line (the underground race: on the top ledge, off the lap) is driven to it after
            // the finish and parked there, each car in its own slot (stopped on the line it blocked the 5 m pipe for everyone still racing)
            if(finished && Race.RouteData!=null && Race.RouteData.finish_park!=null && Race.RouteData.finish_park.Length>=4){ DriveToPark(); return; }
            if (finished && finishRunoff > 55)
            {
                if(Car.ForwardSpeed<.8f && Car.GroundedWheels>=2)
                {
                    Car.Body.linearVelocity=Car.Body.angularVelocity=Vector3.zero;
                    Car.Body.isKinematic=true; finishParked=true;
                }
                else Car.Simulate(0,1,0,Time.fixedDeltaTime);
                return;
            }

            float s = DriveRoad.Project(Car.Body.position, out float lateral);
            float driveHalfWidth=DriveRoad.HalfWidth(s);
            float speed = Mathf.Abs(Car.ForwardSpeed);
            DriveRoad.At(s, out var tangent);
            float look = trail ? Mathf.Clamp(4 + speed * .22f, 5, 11) : Mathf.Clamp(7 + speed * .48f, 8, 25);
            float desiredLane = lane;
            if(finished) { finishRunoff += speed*Time.fixedDeltaTime; desiredLane=Mathf.Min(4.7f,driveHalfWidth); }
            if (!finished)
            {
                bool slowerAhead = false; float slowerLane = 0f, nearest = float.MaxValue;
                foreach (var other in Race.Entrants)
                    if (other != null && other != Racer && other.Car)
                    {
                        var delta = other.Car.transform.position - transform.position;
                        float along = Vector3.Dot(delta, tangent);
                        float side = Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, tangent)));
                        if (along > 4 && along < 50 && side < 3.5f && other.Car.ForwardSpeed < speed + 2)
                        {
                            slowerAhead = true;
                            if (along < nearest) { nearest = along; slowerLane = Vector3.Dot(other.Car.transform.position - DriveRoad.At(s, out _), Vector3.Cross(Vector3.up, tangent).normalized); }
                        }
                    }
                // Chastain Rush, Round 54 (A7: on Addison Heights' open roads the AI queued behind traffic and stalled into resets): a slower traffic
                // car ahead in its lane is passed as a racer is (when the other lane is clear: LaneClear)
                if (!trail && ChastainRush.Traffic.Instance)
                    foreach (var t in ChastainRush.Traffic.Instance.Cars)
                    {
                        if (!t || !t.gameObject.activeSelf) continue;
                        var delta = t.Car.Body.position - transform.position;
                        float along = Vector3.Dot(delta, tangent), side = Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, tangent)));
                        if (along > 4 && along < 50 && side < 3.5f && t.Car.ForwardSpeed < speed + 2) { slowerAhead = true; if (along < nearest) nearest = along; }
                    }
                // Chastain Rush: either side (Racer passes on its left of a two-lane road); Round 19: on a trail, the side away from the
                // slower vehicle (it passed on the side away from its own lane, and when the slower one was on that side it never went)
                if (slowerAhead)
                    desiredLane = trail ? (slowerLane > 0f ? -passLane : passLane) : (lane > 0 ? -passLane : passLane);
                // Commit to a pass only when its destination lane has room alongside and ahead.
                float currentLane=Vector3.Dot(Car.Body.position-DriveRoad.At(s,out _),Vector3.Cross(Vector3.up,tangent).normalized);
                bool laneHeld=false;
                if(Mathf.Abs(desiredLane-currentLane)>1 && !LaneClear(s,desiredLane,speed))
                { laneHeld=true; desiredLane=trail?Mathf.Clamp(currentLane,-passLane,passLane):Mathf.Clamp(currentLane,-driveHalfWidth+1.2f,driveHalfWidth-1.2f); }
                // Chastain Rush, Round 55 (E): a pass is under way while it heads for (or holds) the other lane, clear, past a slower car; it counts once
                // it is back in its own lane with nothing slower ahead
                if (slowerAhead && !laneHeld && Mathf.Abs(desiredLane - lane) > 1f) { passing = true; passLaneNow = desiredLane; }
                else if (laneHeld) passLaneNow = float.NaN;
                else if (passing && !slowerAhead && Mathf.Abs(currentLane - lane) < 1f) { passing = false; passLaneNow = float.NaN; Passes++; }
                if (!slowerAhead && Mathf.Abs(currentLane - lane) < 1f) passLaneNow = float.NaN;
            }

            DecideShortcut(s);
            AiShortcut activeBranch=null;
            if(plannedBranch)
            {
                float toEntry=Mathf.Repeat(plannedBranch.entryRoad-s+DriveRoad.Length*.5f,DriveRoad.Length)-DriveRoad.Length*.5f;
                if(progressBranch==plannedBranch || (toEntry<=2&&toEntry>-15)) activeBranch=plannedBranch;
                else if(toEntry<=-15) plannedBranch=null;   // went past the turn-off without taking it
            }
            // Chastain Rush, Round 40: on a shortcut its station is looked for near where it was (the church leap's line passes under itself in
            // plan: the aisle runs in under the gallery), from its start when it is new
            float branchS=activeBranch?(activeBranch==progressBranch&&branchLast>=0?activeBranch.Line.ProjectNear(Car.Body.position,branchLast,25f,out _):activeBranch.Project(Car.Body.position,out _)):0;
            branchLast=activeBranch?branchS:-1;
            // the end of the shortcut: back to the road
            if(activeBranch && branchS>=activeBranch.Length-3){ plannedBranch=null; activeBranch=null; }
            if(activeBranch!=progressBranch){ if(activeBranch) ShortcutsTaken++; progressBranch=activeBranch;branchBest=branchS;branchStuck=0;}
            if(!finished)TrackRecoveryProgress(activeBranch,activeBranch?branchS:s);
            if(plannedBranch || activeBranch) desiredLane=0;
            float departureDistance=Mathf.Repeat(s-recoveryDepartureStation+DriveRoad.Length*.5f,DriveRoad.Length)-DriveRoad.Length*.5f;
            bool departing=Time.time<recoveryDepartureUntil&&departureDistance<60&&!plannedBranch&&!activeBranch;
            if(departing)desiredLane=Mathf.Clamp(departureLane,-driveHalfWidth+Car.GetComponent<BoxCollider>().size.x*.5f+.5f,driveHalfWidth-Car.GetComponent<BoxCollider>().size.x*.5f-.5f);
            bool nearJump=!finished&&NearZone(s,90f)!=null;   // Chastain Rush, Round 21: the bend mistakes wait until past a jump
            Variation.Step(this,s,speed,finished||plannedBranch||activeBranch||lateral>2.5f||Car.transform.up.y<.9f,nearJump,trail);
            // Chastain Rush, Round 21: the curb clip runs from its lane out to the curb (the car's edge over it: a mailbox, lamp or hydrant there
            // breaks); every other line mistake keeps inside the lane's usual bounds
            if(Variation.Curb)desiredLane=Mathf.Lerp(desiredLane,Mathf.Sign(Variation.Line)*(driveHalfWidth+.15f),Mathf.Abs(Variation.Line));
            else if(Variation.Line!=0)desiredLane=trail?Mathf.Clamp(desiredLane+Variation.Line*.3f,-passLane,passLane):Mathf.Clamp(desiredLane+Variation.Line,-driveHalfWidth+1.6f,driveHalfWidth-1.6f);
            // Chastain Rush, Round 21: a jump mistake, rolled once as it comes up to each jump (ai_zones) on each lap
            if(!finished&&!plannedBranch&&!activeBranch)
            {
                var z=NearZone(s,90f);
                if(z!=null&&(jumpRolled!=z.id||jumpLap!=Racer.CompletedLaps)){jumpRolled=z.id;jumpLap=Racer.CompletedLaps;jumpKind=Variation.RollJump(true,out jumpSide);jumpZone=jumpKind!=0?z:null;}
                if(jumpZone!=null&&NearZone(s,90f)!=jumpZone)jumpZone=null;
                if(jumpZone!=null&&jumpKind==2)desiredLane=trail?Mathf.Clamp(desiredLane+jumpSide*.9f,-passLane-.2f,passLane+.2f):desiredLane+jumpSide*1.2f;
            }
            var target = DriveRoad.At(s + Direction * look, out var ahead);
            target += Vector3.Cross(Vector3.up, ahead).normalized * desiredLane;
            var local = transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, local.z);
            float maxAngle = Mathf.Lerp(Car.slowSteerAngle, Car.fastSteerAngle, Mathf.Clamp01(speed / Car.topSpeed)) * Mathf.Deg2Rad;
            float steering = Mathf.Clamp(Mathf.Atan(2 * Car.wheelbase * Mathf.Sin(angle) / look) / maxAngle, -1, 1);
            if(activeBranch) steering=activeBranch.Steer(Car,branchS,0,out angle);   // Chastain Rush: the shortcut's own line
            else if(trail) steering=trailLine.Steer(Car,s,desiredLane,out angle);   // Round 18: close to the trail's line
            float runS=0;
            if(RunIn&&!activeBranch){ runS=RunIn.Project(Car.Body.position,out _); if(runS>=RunIn.Length-4) RunIn=null; else steering=RunIn.Steer(Car,runS,lane,out angle); }
            int skill = Mathf.Clamp(Difficulty,0,2);
            float cornerGrip = Car.maxGripAcceleration*cornerUse[skill];
            float judgment = Car.braking*brakeUse[skill];
            judgment*=Variation.Judgment;
            TargetSpeed = Car.topSpeed * speedUse[skill] * pace;
            if(departing)TargetSpeed=Mathf.Min(TargetSpeed,26);
            if(finished) TargetSpeed=12;
            // Consistency costs time through early lifting, never extra vehicle capability.
            if (skill < 2 && !activeBranch) TargetSpeed *= 1 - (skill==0?.07f:.012f)*(.5f+.5f*Mathf.Sin(Time.time*.19f+pace*31));
            if(activeBranch) TargetSpeed=Mathf.Min(TargetSpeed,activeBranch.SpeedAt(Car,branchS,cornerGrip,Sloppy));
            else if(plannedBranch&&plannedBranch.entrySpeed>0)
            {
                float toEntry=Mathf.Repeat(plannedBranch.entryRoad-s,DriveRoad.Length);
                TargetSpeed=Mathf.Min(TargetSpeed,Mathf.Sqrt(plannedBranch.entrySpeed*plannedBranch.entrySpeed+2*judgment*Mathf.Max(0,toEntry-12)));
            }
            if(!activeBranch)
                for (float d = 0; d <= 100; d += 8)
                {
                    DriveRoad.At(s + Direction * d, out var f);DriveRoad.At(s + Direction * (d + 8), out var next);
                    float curvature = Vector3.Angle(Vector3.ProjectOnPlane(f,Vector3.up),Vector3.ProjectOnPlane(next,Vector3.up)) * Mathf.Deg2Rad / 8;
                    float curveSpeed = Mathf.Sqrt(cornerGrip / Mathf.Max(.00015f, curvature));
                    var rd = Race ? Race.RouteData : null;   // Chastain Rush, Round 54 (A7): a hilly course's own limits (RouteJson.ai_hill_targets, ai_crest_g)
                    float hillSpeed = Mathf.Abs(f.y) > .14f ? (rd?.ai_hill_targets != null && rd.ai_hill_targets.Length == 3 ? rd.ai_hill_targets[skill] : hillTargets[skill]) : Car.topSpeed;
                    // Without downforce, a convex crest cannot support v²/r greater than gravity.
                    float crest=Mathf.Max(0,Mathf.Asin(f.y)-Mathf.Asin(next.y))/8;
                    if (crest>.001f) hillSpeed=Mathf.Min(hillSpeed,Mathf.Sqrt((rd?.ai_crest_g != null && rd.ai_crest_g.Length == 3 ? rd.ai_crest_g[skill] : skill>0?(skill==2?8.2f:7.6f):6.5f)/crest));
                    TargetSpeed = Mathf.Min(TargetSpeed, Mathf.Sqrt(Mathf.Pow(Mathf.Min(curveSpeed, hillSpeed), 2) + 2 * judgment * Mathf.Max(0, d - 12)));
                }

            if(RunIn) TargetSpeed=Mathf.Min(TargetSpeed,RunIn.SpeedAt(Car,runS,cornerGrip));
            bool held=false;
            var zones=Race.AiZones;   // Chastain Rush, Round 21: the direction's own (reverse Noonday Creek)
            if(zones!=null&&!activeBranch&&!finished)
                foreach(var z in zones)
                {
                    float into=Mathf.Repeat(s-z.s0,DriveRoad.Length), toStart=Mathf.Repeat(z.s0-s,DriveRoad.Length);
                    float zs=z==jumpZone&&jumpKind==4?z.speed*.8f:z.speed;   // Chastain Rush, Round 21: a short jump, a slow run-up
                    if(into<=z.s1-z.s0){TargetSpeed=zs;held=true;}   // the jump's set speed, whatever the crest rule says
                    else if(toStart<90)TargetSpeed=Mathf.Min(TargetSpeed,Mathf.Sqrt(zs*zs+2*judgment*Mathf.Max(0,toStart-6)));
                }
            if (Mathf.Abs(angle) > .6f && !activeBranch && !held)
                TargetSpeed = Mathf.Min(TargetSpeed, 10);
            // Bounded non-alloc obstacle query; includes other cars and solid roadside objects, excludes own body.
            float range = 6 + speed * 1.5f;
            float radius=Mathf.Min(.7f,Car.GetComponent<BoxCollider>().size.x*.5f+.12f);
            int count = Physics.SphereCastNonAlloc(Car.Body.position + Vector3.up * .35f, radius, transform.forward, hits, range, ~0, QueryTriggerInteraction.Ignore);
            LastObstacle=""; obstacleVehicle=false;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.rigidbody == Car.Body || hit.normal.y > .55f || hit.collider is TerrainCollider)
                    continue;
                // Chastain Rush: on a shortcut (and in its run-up) the static world is the line itself: ramps, the stairs, the
                // dirt pile, the door frame it is aimed through. Only other vehicles count there (Racer: "the obstacle sensor
                // ignores static ground inside a committed run-up").
                if((activeBranch||trail||(plannedBranch&&Mathf.Repeat(plannedBranch.entryRoad-s,DriveRoad.Length)<40))&&!hit.rigidbody) continue;   // Round 18: on the trail too
                float aheadSpeed=hit.rigidbody?Mathf.Max(0,Vector3.Dot(hit.rigidbody.linearVelocity,transform.forward)):0;
                // Chastain Rush, Round 55 (E): a car it is pulling out round (the pass lane chosen and clear) does not stop it: it goes on past at
                // 4 m/s more than that car, never stopping on its bumper; a car it cannot pass yet it follows a car length back (8 m to the hit, Racer's 4 m: room to pull out)
                float keep = 4f, floor = 0f;
                if (PassFix && hit.rigidbody && hit.rigidbody != Car.Body)
                {
                    float hitLane = Vector3.Dot(hit.rigidbody.position - DriveRoad.At(s, out _), Vector3.Cross(Vector3.up, tangent).normalized);
                    if (!float.IsNaN(passLaneNow) && Mathf.Abs(hitLane - passLaneNow) > 2.1f) floor = aheadSpeed + 4f;   // pulling out round it: on past it
                    else keep = 8f;
                }
                float allowed = Mathf.Max(floor, Mathf.Sqrt(Mathf.Max(0,aheadSpeed*aheadSpeed+2*judgment*(hit.distance-keep))));
                if(allowed<TargetSpeed){LastObstacle=hit.collider.name+"@"+hit.distance.ToString("F1");obstacleVehicle=hit.rigidbody!=null;}
                TargetSpeed = Mathf.Min(TargetSpeed, allowed);
            }

            float throttle = speed < TargetSpeed ? Mathf.Clamp01((TargetSpeed - speed) * (skill>0?.8f:.5f)) : 0;
            float brake = speed > TargetSpeed + .5f ? Mathf.Clamp01((speed - TargetSpeed) * .25f) : 0;
            if((activeBranch||held)&&speed<TargetSpeed) throttle=1;   // the run-up and jump at full throttle (Racer: aiEntrySpeed)
            if (TargetSpeed < 1 && speed < .7f)
            {
                throttle = brake = 0;
                Car.Body.linearVelocity *= .85f;
            }

            if (speed < 1.5f)
                stalled += Time.fixedDeltaTime;
            else
                stalled = 0;
            if ((stalled > 5 && stalled < 8) || (routeStuck>8 && routeStuck<11))
            {
                throttle = 0;
                brake = .45f;
                steering = -steering;
            }

            // Racer 0.72: a rival below the world is restored to the nearest course point at once
            if(!finished&&Car.Body.position.y<RaceRoute.Surface(Car.Body.position.x,Car.Body.position.z).y-20&&Time.time>=nextRecovery)TryRecover(s);
            // Chastain Rush, Round 21: wading out of R1's pool is not being stuck (for 12 s; longer, the usual recovery)
            wadeTime=PoolHold.Wading(Car)?wadeTime+Time.fixedDeltaTime:0f;
            if(wadeTime>0f&&wadeTime<12f){stalled=0;routeStuck=branchStuck=rejoinStuck=0;}
            float noProgress=Mathf.Max(routeStuck,branchStuck,rejoinStuck);
            if(!finished) LongestStall=Mathf.Max(LongestStall,Mathf.Max(noProgress,stalled));
            bool racerStuck=!finished && (noProgress>8 ||
                (noProgress>5&&(speed<2||trackingRejoin||transform.up.y<.35f)));
            if (Time.time>=nextRecovery && racerStuck)
                TryRecover(s);

            LastThrottle=throttle; LastBrake=brake; if(brake>.1f) BrakingSeconds+=Time.fixedDeltaTime;
            if(!finished&&obstacleVehicle&&speed<10f) BehindSeconds+=Time.fixedDeltaTime;   // Round 55 (E): close behind a car, slow
            Car.Simulate(throttle, brake, steering, Time.fixedDeltaTime);
        }

        int parkIndex=-1, parkSlot; Vector3[] park;
        void DriveToPark()
        {
            if(park==null)
            {
                var j=Race.RouteData; int n=j.finish_park.Length/2; park=new Vector3[n];
                for(int i=0;i<n;i++) park[i]=new Vector3(j.finish_park[2*i], j.finish_park_y!=null&&j.finish_park_y.Length==n?j.finish_park_y[i]:Car.Body.position.y, j.finish_park[2*i+1]);
                parkSlot=Race.TakeParkSlot(); parkIndex=0;
                // the slot: 9 m back from the line's end per car already parked
                float back=(j.finish_park_gap_m>0f?j.finish_park_gap_m:9f)*parkSlot; for(int i=n-1;i>0&&back>0f;i--){ float L=Vector3.Distance(park[i],park[i-1]); if(L>=back){ park[i]=Vector3.Lerp(park[i],park[i-1],back/L); break; } back-=L; n=i; }
                System.Array.Resize(ref park,n);
            }
            var at=Car.Body.position; var target=park[Mathf.Min(parkIndex,park.Length-1)];
            if(parkIndex<park.Length-1 && Vector3.Distance(new Vector3(at.x,0,at.z),new Vector3(target.x,0,target.z))<4f) { parkIndex++; target=park[parkIndex]; }
            var to=target-at; to.y=0; float dist=to.magnitude, speed=Car.ForwardSpeed;
            float steer=Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(Car.transform.forward,Vector3.up),to,Vector3.up)/25f,-1,1);
            bool last=parkIndex>=park.Length-1; float want=last?Mathf.Clamp(dist*0.8f,0f,8f):(Race.RouteData.finish_park_speed>0f?Race.RouteData.finish_park_speed:9f);
            if(last&&dist<1.5f&&speed<0.8f&&Car.GroundedWheels>=2){ Car.Body.linearVelocity=Car.Body.angularVelocity=Vector3.zero; Car.Body.isKinematic=true; finishParked=true; return; }
            Car.Simulate(speed<want?1f:0f, speed>want+1f?1f:0f, steer, Time.fixedDeltaTime);
        }

        void DecideShortcut(float s)
        {
            if(Racer.Finished || !Racer.ClockStarted || plannedBranch) return;
            foreach(var branch in Race.AiShortcuts)
            {
                float toEntry=Mathf.Repeat(branch.entryRoad-s,DriveRoad.Length);
                if(toEntry>55 || toEntry<5) continue;
                if(Racer.NextCheckpoint>branch.FirstForgiven) continue;   // Racer: only when the racer's next gate is the one the branch bypasses
                if(ForcedShortcut!=null){ if(branch==ForcedShortcut){plannedBranch=branch;Sloppy=branch.slip>0&&ForceSloppy==true;break;} continue; }
                if(!branch.Allows(Car) || Shortcuts.Decided(branch,Racer.CompletedLaps)) continue;
                if(Occupied(branch)) continue;   // Chastain Rush: another car at the turn-off or on the line: keep to the road for now
                var order=Race.Ordered();int place=order.IndexOf(Racer);
                float rank=order.Count>1?(float)place/(order.Count-1):.5f;
                var leader=order[0];float gap=Mathf.Max(0,Race.Score(leader)-Race.Score(Racer));
                if(Shortcuts.TryDecide(branch,Racer.CompletedLaps,rank,gap,out bool take,CheckTakeAll?1f:branch.take)&&take)
                {
                    plannedBranch=branch;
                    // Chastain Rush, Round 20 follow-ups: a sloppy try (R1's pool); Round 21: more often on Easy, less on Hard
                    Sloppy=branch.slip>0&&(ForceSloppy??Shortcuts.Roll(branch.slip*(Difficulty==0?1.5f:Difficulty==2?.6f:1f)));
                    if(Sloppy){SlipsTaken++;Variation.CountSlip();}
                    break;
                }
            }
        }

        // A projection beneath an elevated road is not meaningful route progress.
        // Off-route cars instead get credit for approaching a fixed rejoin target;
        // reversing/oscillating around the same hillside cannot keep resetting it.
        void TrackRecoveryProgress(AiShortcut branch,float station)
        {
            var p=Car.Body.position;
            var support=branch?branch.At(station,out _):DriveRoad.At(station,out _);
            if(Car.GroundedWheels==0&&Car.Body.linearVelocity.magnitude>3&&p.y>support.y-15){airborne=true;return;}
            if(airborne){airborne=false;trackingRejoin=false;rejoinStuck=0;}
            float width=branch?branch.halfWidth:DriveRoad.HalfWidth(station);
            bool near=Vector3.ProjectOnPlane(p-support,Vector3.up).magnitude<=width+3
                &&Mathf.Abs(p.y-support.y)<6;
            if(!near){
                routeStuck=branchStuck=0;
                if(!branch){
                    float tracked=DriveRoad.ProjectNear(p,progressStation,60,out _);var trackedPoint=DriveRoad.At(tracked,out _);
                    float advance=Mathf.Repeat(tracked-progressStation+DriveRoad.Length*.5f,DriveRoad.Length)-DriveRoad.Length*.5f;
                    bool onCourse=Mathf.Abs(p.y-trackedPoint.y)<12&&Vector3.ProjectOnPlane(p-trackedPoint,Vector3.up).magnitude<DriveRoad.HalfWidth(tracked)+15;
                    if(progressSample&&onCourse&&advance>2){progressStation=tracked;trackingRejoin=false;rejoinStuck=0;return;}
                }
                if(!trackingRejoin){trackingRejoin=true;rejoinTarget=support;rejoinBestDistance=Vector3.Distance(p,support);rejoinStuck=0;}
                float distance=Vector3.Distance(p,rejoinTarget);
                if(distance<rejoinBestDistance-2){rejoinBestDistance=distance;rejoinStuck=0;}
                else rejoinStuck+=Time.fixedDeltaTime;
                return;
            }
            trackingRejoin=false;rejoinStuck=0;
            if(branch){
                routeStuck=0;
                if(station>branchBest+2){branchBest=station;branchStuck=0;}else branchStuck+=Time.fixedDeltaTime;
            }else{
                branchStuck=0;
                float delta=progressSample?Mathf.Repeat(station-progressStation+DriveRoad.Length*.5f,DriveRoad.Length)-DriveRoad.Length*.5f:3;
                if(delta>2){progressStation=station;routeStuck=0;progressSample=true;}else routeStuck+=Time.fixedDeltaTime;
            }
        }

        // Chastain Rush: Racer's VehicleRespawn.TryRecoverLocal is not imported. The car goes back on its shortcut's line (a few
        // metres behind the furthest point it reached) or on the lap at its own station, facing the way of travel, on the first
        // spot whose box touches nothing but the ground and is not in water (CarRecovery's rule for the player).
        AiShortcut lastRecoverBranch;   // Chastain Rush, Round 28
        int unplaced;                   // Chastain Rush, Round 49: tries in a row that found no clear spot
        float lastRecoverStation=-999f,lastRecoverTime=-999f;   // Chastain Rush, Round 57: where and when it last needed a recovery
        void TryRecover(float current)
        {
            nextRecovery=Time.time+1;
            Debug.Log($"[AI] {name} recovery: at ({Car.Body.position.x:0.0},{Car.Body.position.z:0.0}) speed {Car.Body.linearVelocity.magnitude:0.0}, station {current:0}, stalled {stalled:0.0} s, no progress route {routeStuck:0.0} / shortcut {branchStuck:0.0} / rejoin {rejoinStuck:0.0} s, planned {(plannedBranch?plannedBranch.Id:"-")}, on {(progressBranch?progressBranch.Id:"-")}, obstacle '{LastObstacle}', tilt {Vector3.Angle(transform.up,Vector3.up):0}");
            Vector3 at=default; Vector3 fwd=default; bool found=false;
            // Chastain Rush: held up on a shortcut by another vehicle (the lines are one car wide): back to the lap, not onto
            // the same line behind it
            if(progressBranch&&obstacleVehicle) progressBranch=null;
            // Chastain Rush, Round 28: stuck again on the same shortcut (its line put it back where it stuck): give it up, back to the lap
            // (the underground race's room turns; never a loop of resets)
            if(progressBranch&&progressBranch==lastRecoverBranch) progressBranch=null;
            lastRecoverBranch=progressBranch;
            if(progressBranch)
                foreach(float back in new[]{6f,10f,16f})
                {
                    var p=progressBranch.At(Mathf.Max(0,branchBest-back),out var f);
                    if(Clear(p,f)){at=p;fwd=f;found=true;break;}
                }
            // Chastain Rush, Round 57 (C, Dan BUG-004 in M5c: "the tube is completley blocked", an AI ATV stopped in the pipe): stuck again within
            // 15 m of where it last stuck (put back in the same trap, between two blockages 10 m apart): this time only further on, 20 m and more
            bool again=Mathf.Abs(current-lastRecoverStation)<15f&&Time.time-lastRecoverTime<40f;
            lastRecoverStation=current; lastRecoverTime=Time.time;
            if(!found)
            {
                plannedBranch=null; progressBranch=null;
                foreach(float ds in again?new[]{20f,26f,34f,46f,60f,80f,100f}:new[]{0f,-4f,4f,-8f,8f,-12f,12f,-18f,18f,26f,34f,-26f,46f,60f})   // Chastain Rush, Round 28: further on too (a jam there); Round 49: and further still
                {
                    // Chastain Rush, Round 28: never back across the start / finish line (driving over it again counts a lap with every
                    // checkpoint missed; at the underground race's start the AI jam in M5's mouth, right at the line)
                    if((current<20f&&current+ds<1f)||(current>DriveRoad.Length-20f&&current+ds>DriveRoad.Length-1f)) continue;
                    foreach(float off in trail?new[]{0f,lane,-lane}:new[]{lane,0f,-lane,2.5f,-2.5f})
                    {
                        var p=DriveRoad.At(current+ds,out var f); f=Vector3.ProjectOnPlane(f,Vector3.up).normalized;
                        p+=Vector3.Cross(Vector3.up,f)*off; p=RaceRoute.Surface(p.x,p.z,p.y);   // Chastain Rush, Round 27: Surface (the underground race); Round 28: at the lap's own height there
                        if(Clear(p,f)){at=p;fwd=f;found=true;break;}
                    }
                    if(found)break;
                }
            }
            // Chastain Rush, Round 49 check (a mower wedged under the lake trail's footbridge, every spot within 34 m touching the bridge, the rails or
            // the water: 180 tries, never moved): after four tries with no clear spot it goes back on the lap's middle 20 m on anyway (never a trap)
            if(!found)
            {
                if(++unplaced<4) return;
                for(float ds=20f;ds<=120f&&!found;ds+=10f)
                {
                    var p0=DriveRoad.At(current+ds,out var f0); f0=Vector3.ProjectOnPlane(f0,Vector3.up).normalized; var q=RaceRoute.Surface(p0.x,p0.z,p0.y);
                    if(!Covered(q)&&!ShallowWater.InWater(q+Vector3.up*.1f)){at=q;fwd=f0;found=true;}
                }
                if(!found) return;
                plannedBranch=null; progressBranch=null;
            }
            unplaced=0;
            var rot=Quaternion.LookRotation(fwd,Vector3.up);
            Car.Body.position=at+Vector3.up*.5f; Car.Body.rotation=rot; transform.SetPositionAndRotation(Car.Body.position,rot);
            Car.Body.linearVelocity=Car.Body.angularVelocity=Vector3.zero; Car.ClearSteering(); Physics.SyncTransforms();
            Racer.NotifyTeleported();
            stalled=branchStuck=routeStuck=rejoinStuck=0;trackingRejoin=false;progressStation=DriveRoad.Project(Car.Body.position,out _);progressSample=true;branchBest=Mathf.Max(0,branchBest-6); nextRecovery=Time.time+4; RecoveryCount++;
            recoveryDepartureStation=progressStation;recoveryDepartureUntil=Time.time+8;
            var support=DriveRoad.At(progressStation,out var departureForward);departureLane=Vector3.Dot(Car.Body.position-support,Vector3.Cross(Vector3.up,departureForward).normalized);
            if(!progressBranch)lane=lane>0?-laneSide:laneSide;
        }

        // Chastain Rush, Round 21: the ai_zone (a jump) this car is in, or within 'ahead' metres before
        RouteAiZoneJson NearZone(float s,float ahead)
        {
            var zones=Race.AiZones; if(zones==null) return null;
            foreach(var z in zones)
            {
                float into=Mathf.Repeat(s-z.s0,DriveRoad.Length), toStart=Mathf.Repeat(z.s0-s,DriveRoad.Length);
                if(into<=z.s1-z.s0+4f||toStart<ahead) return z;
            }
            return null;
        }

        // another racer within the first 80 m of the shortcut's line, or near its turn-off ahead of this car
        bool Occupied(AiShortcut branch)
        {
            foreach(var other in Race.Entrants)
            {
                if(other==null||other==Racer||!other.Car) continue;
                var p=other.Car.Body.position;
                float bs=branch.Project(p,out float lat);
                if(lat<6f&&bs<80f) return true;
            }
            return false;
        }

        // Chastain Rush, Round 49 check: a spot with something over it (the lake trail's footbridge: the lap's height there is the ground under the
        // deck, the stream bed; two AI cars were put back under it again and again). Not in the underground race (its pipes have a ceiling)
        bool Covered(Vector3 q)
        {
            if(RaceRoute.Active!=null&&RaceRoute.Active.Underground) return false;
            return Physics.Raycast(q+Vector3.up*.6f,Vector3.up,out var hit,8f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.attachedRigidbody==null&&!(hit.collider is TerrainCollider);
        }
        bool Clear(Vector3 q,Vector3 f)
        {
            if(ShallowWater.InWater(q+Vector3.up*.1f)) return false;
            if(Covered(q)) return false;
            var rot=Quaternion.LookRotation(Vector3.ProjectOnPlane(f,Vector3.up).normalized,Vector3.up);
            var box=Car.GetComponent<BoxCollider>(); var half=box?box.size*.5f+new Vector3(.3f,0,.4f):new Vector3(1.3f,.7f,2.4f);
            int n=Physics.OverlapBoxNonAlloc(q+Vector3.up*(half.y+.35f),half,overlap,rot,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++){var h=overlap[i]; if(h is TerrainCollider||h.attachedRigidbody==Car.Body) continue; return false;}
            return true;
        }

        bool LaneClear(float s,float targetLane,float speed)
        {
            var center=DriveRoad.At(s,out var forward);var right=Vector3.Cross(Vector3.up,forward).normalized;
            bool Blocks(ArcadeVehicle other)
            {
                var delta=other.Body.position-Car.Body.position;float along=Vector3.Dot(delta,forward);
                float lanePosition=Vector3.Dot(other.Body.position-center,right);
                return along>-12&&along<Mathf.Max(18,speed*.9f)&&Mathf.Abs(lanePosition-targetLane)<laneGap;
            }
            foreach(var other in Race.Entrants)if(other!=null&&other!=Racer&&other.Car&&Blocks(other.Car))return false;
            // Chastain Rush, Round 54 (A7): and the traffic in that lane, an oncoming car as far ahead as it closes in two and a half seconds
            if(ChastainRush.Traffic.Instance)
                foreach(var t in ChastainRush.Traffic.Instance.Cars)
                {
                    if(!t||!t.gameObject.activeSelf) continue;
                    var b=t.Car.Body; var delta=b.position-Car.Body.position; float along=Vector3.Dot(delta,forward), lanePosition=Vector3.Dot(b.position-center,right);
                    float closing=Mathf.Max(0,speed-Vector3.Dot(b.linearVelocity,forward));
                    if(along>-12&&along<Mathf.Max(18,speed*.9f)+closing*2.5f&&Mathf.Abs(lanePosition-targetLane)<laneGap+.5f) return false;
                }
            return true;
        }
    }

    // Chastain Rush, Round 15: an official shortcut as the AI drives it (Racer's WoodlandRoute, the parts RoadDriver uses):
    // its line as an open road, where it leaves (entryRoad) and rejoins (exitRoad) the race road, the speed to arrive with
    // and hold through its run-up and jump (entrySpeed, Racer's aiEntrySpeed), the vehicles it is validated for, and the
    // checkpoints it forgives (from RaceShortcut).
    public sealed class AiShortcut
    {
        public string Id, Name;
        public RaceRoad Line;
        public float entryRoad, exitRoad, entrySpeed, halfWidth = 3.5f;
        public float[] profile;   // Chastain Rush, Round 18: (station, speed) pairs along the line, no faster there (null: none)
        public float take, slip;  // Chastain Rush, Round 20 follow-ups: its own chance of being taken (0: the strategy's), of a sloppy try
        public float[] slipProfile;   // the speeds of a sloppy try (R1: too slow off the ramp, into the pool)
        public int FirstForgiven;
        public System.Func<ArcadeVehicle,bool> Allows = _ => true;
        public float Length => Line.Length;
        public float Project(Vector3 p, out float lateral) => Line.Project(p, out lateral);
        public Vector3 At(float s, out Vector3 forward) => Line.At(s, out forward);
        public static implicit operator bool(AiShortcut a) => a != null;

        // the speed to hold here: the entry speed, no faster than the line's bends allow (the shortcut check's rule: v at most
        // sqrt(grip x radius) at each bend ahead, reached in time by braking)
        public float SpeedAt(ArcadeVehicle car, float s, float grip, bool sloppy = false)
        {
            var profile = sloppy && slipProfile != null && slipProfile.Length >= 2 ? slipProfile : this.profile;
            float w = entrySpeed > 0 ? entrySpeed : car.topSpeed;
            for (float d = 6f; d < 60f && s + d + 6f < Length; d += 2f)
            {
                Vector3 a = At(s + d - 6f, out _), b = At(s + d, out _), c = At(s + d + 6f, out _);
                a.y = b.y = c.y = 0f;
                float area2 = Mathf.Abs(Vector3.Cross(b - a, c - a).y); if (area2 < 1e-3f) continue;
                float r = (b - a).magnitude * (c - b).magnitude * (c - a).magnitude / (2f * area2);
                float vb = Mathf.Sqrt(grip * r);
                w = Mathf.Min(w, Mathf.Sqrt(vb * vb + 2f * car.braking * .88f * Mathf.Max(0f, d - 6f)));
            }
            if (profile != null)   // Round 18: the line's own speeds, braked for in time, held between two points
                for (int i = 0; i + 1 < profile.Length; i += 2)
                {
                    float ps = profile[i], pv = profile[i + 1];
                    bool next = i + 3 < profile.Length;
                    if (s <= ps) w = Mathf.Min(w, Mathf.Sqrt(pv * pv + 2f * car.braking * .88f * (ps - s)));
                    else if (next && s < profile[i + 2]) w = Mathf.Min(w, Mathf.Lerp(pv, profile[i + 3], Mathf.InverseLerp(ps, profile[i + 2], s)));
                }
            return w;
        }

        // steering along the line (or 'offset' metres to its right): heading error to a point a few metres ahead plus a
        // cross-track term (ShortcutCheckRunner's)
        public float Steer(ArcadeVehicle car, float s, float offset, out float angle)
        {
            float v = Mathf.Max(car.ForwardSpeed, 1f);
            var p = At(s, out var f); f = Vector3.ProjectOnPlane(f, Vector3.up).normalized;
            var aim = At(s + 4f + .12f * v, out _);
            var pos = car.Body.position;
            float cte = Vector3.Dot(Vector3.ProjectOnPlane(pos - p, Vector3.up), Vector3.Cross(Vector3.up, f)) - offset;   // + = right of the line (offset: a lane to its right)
            var fwd = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized;
            var lineDir = Vector3.ProjectOnPlane(aim - p, Vector3.up); if (lineDir.sqrMagnitude < 1e-4f) lineDir = f;
            float head = Vector3.SignedAngle(fwd, lineDir.normalized, Vector3.up);
            float corr = -Mathf.Atan2(1.6f * cte, v + 2f) * Mathf.Rad2Deg;
            angle = (head + corr) * Mathf.Deg2Rad;
            return Mathf.Clamp((head + corr) / 18f, -1f, 1f);
        }
    }
}
