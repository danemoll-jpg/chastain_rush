using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): Racer's Getaway (0.96 Part E, with its 0.97 to 0.99 fixes), the AI cops chasing you, in Free Roam on this map.
    //   Free Roam only, never in a race; started from the POLICE page (one player on the full screen, or two in split screen, the AI as a second
    //   runner if chosen) or by a hidden cruiser (HiddenPolice: Embedded, where the player is, no records); solo play never swaps roles
    //   The chase starts at once with a cop behind you (lights and siren on, 60-80 m back); heat 1 to 5 (+1 every HeatEvery s while chased; on Normal
    //   and Hard also ramming a cop, running a roadblock, 10 s off-road while seen), a cop per level, backups placed ahead out of sight, exits covered
    //   when you go off-road, roadblocks from heat 3, the helicopter from heat 4; the escape meter fills while no cop or the helicopter sees you;
    //   the bust meter as Racer's. Every rise is announced (the big alert and the radio).
    //   This map: the cops drive RoadNet (every road of the map); the radio names streets (PoliceRadio: the street a place is on, the suspect's
    //   heading on it, turning onto a street).
    //   Underground (decided, Round 39): the cops never follow into the pipe network. Going down a manhole or a pipe's mouth is out of every cop's and
    //   the helicopter's sight ("Suspect went underground. We can't follow."), the escape meter fills twice as fast down there and the bust meter cannot
    //   fill; the cops search the streets round where you went down and the backups are posted ahead of your heading; coming up again where a cop
    //   sees you: "Suspect's back up on the street." The stunt park, the creek trail and the shortcuts are fair ground (the cops cover the exits there).
    public sealed class GetawayChase : MonoBehaviour
    {
        public const float SightRange = 120, NearCops = 150, CatchDistance = 10, CatchSpeed = 7, BoxSpeed = 18, ContactSpeed = 22, PinDistance = 20, PinSpeed = 99, HeldSpeed = 15, HeldDistance = 20, RunnerRelease = 3, EndPause = 4, ResetHold = 2;
        public static GetawayChase Current { get; private set; }
        public static System.Action<GetawayChase> Finished;   // the menus show the results (not an embedded chase)
        public enum Phase { Starting, Running, Over, Done }
        public sealed class Runner
        {
            public int player; public ArcadeVehicle car; public bool human; public Rigidbody body;
            public float bust, escape, freeSeconds, endedAt, seenAt = -99, lastSeenAt = -99, offSince = -1, onSince, holdUntil, contactAt = -99, unseenFor, offSeen, lastCallIn = -99, lastRam = -99, nextHeading = 25;
            public Vector3 lastSeenPos, lastSeenVelocity, offFrom, prevVel; public int offFromNode = -1; public string street;
            public bool seen, offRoad, caught, escaped, wasSeen, heliSees, offBumped, underground, wasUnder; public int topHeat = 1, dodged, blocks, copsChasing; public string outcome = "";
            public bool Done => caught || escaped; public bool limit; public System.Action resetHook;
        }
        public sealed class Cop
        {
            public int id; public ArcadeVehicle car; public CopDriver driver; public PoliceLights lights; public Runner assigned; public bool sees; public bool block, exitUnit, catching; public float closeAt = -99, bornAt, boost = 1, farSince = -1; public bool wasClose; public float nextPlan, searchUntil;
            public Vector3 Position => car ? car.Body.position : Vector3.zero;
        }
        public sealed class Roadblock { public Vector3 centre, tangent; public Cop a, b; public float born; public Runner target; public bool passed; public float closest = 999; public int node; }
        public readonly List<Runner> Runners = new(); public readonly List<Cop> Cops = new(); public readonly List<Roadblock> Blocks = new();
        public Phase State { get; private set; }
        public RoadNet Net { get; private set; }
        public float Clock { get; private set; }
        public int Heat { get; private set; } = 1;
        public float StartLeft => State == Phase.Starting ? Mathf.Max(0, RunnerRelease - (Time.time - phaseAt)) : 0;
        public bool Embedded { get; private set; } int embeddedDifficulty = 1; public string Banner = ""; public float BannerAt = -99; public const float EmbeddedEndPause = 3.2f;
        public float Limit => Embedded ? 300 : PoliceSetup.Minutes * 60;
        public int Difficulty => Embedded ? Mathf.Clamp(embeddedDifficulty, 0, 2) : Mathf.Clamp(PoliceSetup.Difficulty, 0, 2);
        public static readonly int[] CopCap = { 4, 6, 6 }; public static readonly float[] HeatEvery = { 45, 30, 20 }; public static readonly float[] EscapeBy = { 12, 30, 40 }; public static readonly float[] BustRate = { .8f, 1f, 1.25f };
        // Chastain Rush, Round 39 (D5, checked like Dan plays): Racer's catch-up (up to 1.3 x when a cop is over 150 m back) kept every cop within the
        // 150 m an escape needs, so on the roads nobody ever got away. Easy cops drive at 92 % and catch up at most 1.08 x, Normal at most 1.2 x; Hard as Racer
        public static readonly float[] CopPace = { .92f, 1f, 1f }, CatchUp = { .08f, .2f, .3f };
        // Round 39: Easy escapes after 12 s out of sight (Racer 20)
        public static readonly float[] NearCopsBy = { 110, NearCops, NearCops }, SightBy = { 90, SightRange, SightRange };   // and on Easy a cop sees you within 90 m (120 as Racer)   // Round 39: on Easy no cop within 110 m (150 as Racer on Normal and Hard)
        public int PeakCops { get; private set; }
        public int MaxCops => CopCap[Difficulty];
        public readonly List<string> TraceLog = new(); void Trace(string s) { TraceLog.Add(s); if (TraceLog.Count > 200) TraceLog.RemoveAt(0); }
        public List<string> RadioLog = new(); public string Radio => RadioLog.Count > 0 && Time.unscaledTime - radioAt < RadioSeconds ? RadioLog[RadioLog.Count - 1] : ""; float radioAt; public const float RadioSeconds = 4, AlertSeconds = 4.5f;
        public string Alert = ""; public float AlertAt = -99; public bool AlertShown => Alert != "" && Time.unscaledTime - AlertAt < AlertSeconds;
        public readonly List<Vector3> ExitPoints = new();
        public PoliceHelicopter Heli; float nextPrune; float phaseAt, nextTick, heatClock, nextBlock, nextBackup; int copSerial; readonly HashSet<ArcadeVehicle> held = new();
        public bool Holds(ArcadeVehicle car) => held.Contains(car);
        public bool IsCop(Rigidbody body) => body && Cops.Any(c => c.car && c.car.Body == body);
        public static bool CheckFast;   // checks only: faster heat (never set in play)

        // ---------- starting ----------
        public static GetawayChase Begin()
        {
            End(); var g = new GameObject("Getaway").AddComponent<GetawayChase>(); Current = g; g.BeginRound(); return g;
        }
        public static void End() { if (Current) { Current.Release(); Destroy(Current.gameObject); Current = null; } }
        void OnDestroy() { Release(); ClearCops(); if (Current == this) Current = null; PoliceRadio.Clear(); }
        void Release() { foreach (var c in held.ToList()) PoliceKit.Hold(held, c, false, IsAi(c)); foreach (var r in Runners) { var rs = r.car ? r.car.GetComponent<VehicleRespawn>() : null; if (rs && r.resetHook != null) rs.Respawned -= r.resetHook; } }
        bool IsAi(ArcadeVehicle c) => Runners.Any(r => r.car == c && !r.human);
        public Runner RunnerOf(int player) => Runners.FirstOrDefault(r => r.player == player);
        public static string Mss(float seconds) => PoliceKit.Mss(seconds);

        public void BeginRound()
        {
            ClearCops(); Runners.Clear(); RadioLog.Clear(); ExitPoints.Clear();
            Net = RoadNet.Shared;
            var car1 = PoliceKit.Player(1);
            // the runners on the road nearest the player, facing along it; player 2 beside
            int node = Net.Nearest(car1.Body.position, out _, 400); var f = node >= 0 ? Net.Tangent(node) : car1.transform.forward; f.y = 0;
            if (Vector3.Dot(f, car1.transform.forward) < 0) f = -f; var p = node >= 0 ? Net.P[node] : car1.Body.position; var right = Vector3.Cross(Vector3.up, f).normalized;
            float side = node >= 0 ? Mathf.Max(1.4f, Net.Half[node] * .5f) : 2.4f;
            var r1 = new Runner { player = 1, car = car1, human = true }; Runners.Add(r1); PoliceKit.Put(car1, p + right * side, f);
            var car2 = SplitScreen.Active ? SplitScreen.P2Car : null;
            if (car2) { bool ai = SplitScreen.P2Ai; var r2 = new Runner { player = 2, car = car2, human = !ai }; Runners.Add(r2); PoliceKit.Put(car2, p - right * side, f); if (ai) SplitScreen.Session.Cruise(); }
            foreach (var r in Runners)
            {
                r.body = r.car.Body; PoliceKit.Hold(held, r.car, true, !r.human);
                var respawn = r.car.GetComponent<VehicleRespawn>(); var capture = r; System.Action reset = () => RunnerReset(capture);
                if (respawn) { respawn.Respawned -= r.resetHook; r.resetHook = reset; respawn.Respawned += reset; }
            }
            Clock = 0; Heat = 1; heatFiveAt = -1; heatFiveSaid = false; heliSaw = false; heliCalm = 0; nextLoud = 20; heatClock = 0; nextBlock = 0; nextBackup = 0; nextPrune = 0; Heli = null; State = Phase.Starting; phaseAt = Time.time; nextTick = 0; copSerial = 0; newUnits = 0;
            // the first cops behind the runner: one already in pursuit 60-80 m back, a second further back (held until the runners go)
            for (int i = 0; i < StartingCops(); i++)
            {
                float back = i == 0 ? Random.Range(62f, 78f) : 120 + 45 * (i - 1);
                int at = node >= 0 ? Net.Ahead(node, -f, back) : -1;
                var cop = SpawnCop(at >= 0 ? Net.P[at] : p - f * back, at >= 0 ? TowardsHeading(at, f) : f); if (cop != null) { cop.driver.Frozen = true; cop.lights.Siren = true; }
            }
            Alert = "HEAT 1 — unit in pursuit"; AlertAt = Time.unscaledTime; Say("R01", 1, true);
        }
        // a hidden cruiser pulls out (the parked car it replaces is already gone) and the chase starts where the player is
        public static GetawayChase BeginEmbedded(RoadNet net, Vector3 at, Vector3 forward, int difficulty)
        {
            End(); var g = new GameObject("Getaway (hidden police)").AddComponent<GetawayChase>(); Current = g; g.Embedded = true; g.embeddedDifficulty = difficulty; g.BeginEmbeddedRound(net, at, forward); return g;
        }
        void BeginEmbeddedRound(RoadNet net, Vector3 at, Vector3 forward)
        {
            ClearCops(); Runners.Clear(); RadioLog.Clear(); ExitPoints.Clear(); Net = net ?? RoadNet.Shared;
            var car = PoliceKit.Player(1); var r1 = new Runner { player = 1, car = car, human = true, body = car.Body }; Runners.Add(r1);
            var respawn = car.GetComponent<VehicleRespawn>(); System.Action reset = () => RunnerReset(r1); if (respawn) { r1.resetHook = reset; respawn.Respawned += reset; }
            Clock = 0; Heat = 1; heatFiveAt = -1; heatFiveSaid = false; heliSaw = false; heliCalm = 0; nextLoud = 25; heatClock = 0; nextBlock = 0; nextBackup = 2; nextPrune = 0; Heli = null; State = Phase.Running; phaseAt = Time.time; nextTick = 0; copSerial = 0; newUnits = 0;
            var cop = SpawnCop(at, forward); if (cop != null) { cop.driver.Frozen = false; cop.lights.Siren = true; }
            Alert = "HEAT 1 — unit in pursuit"; AlertAt = Time.unscaledTime; Say("H02", 1, true);
        }
        int StartingCops() => Mathf.Min(MaxCops, 2);
        Vector3 TowardsHeading(int node, Vector3 f) { var t = Net.Tangent(node); return Vector3.Dot(t, f) < 0 ? -t : t; }
        void RunnerReset(Runner r) { if (State != Phase.Running || r.Done) return; r.holdUntil = Time.time + ResetHold; PoliceKit.Hold(held, r.car, true, !r.human); }

        // ---------- the cops ----------
        int newUnits;
        Cop SpawnCop(Vector3 at, Vector3 forward, bool quiet = false, bool allowBike = true)
        {
            var template = PoliceKit.Player(1); if (!template) return null;
            bool bike = allowBike && Heat >= 3 && ++newUnits % 3 == 0;   // 0.99 Part E: from heat 3 one in three new units is a police bike
            var car = PoliceKit.MakePatrol(template, bike ? "policebike" : "police", at, forward, "COP " + (++copSerial));
            MatchRunner(car);
            var driver = car.gameObject.AddComponent<CopDriver>(); driver.BaseTop = car.topSpeed; driver.Car = car; driver.Net = Net; driver.Boss = this; driver.SpeedFactor = CopPace[Difficulty];
            var lights = car.gameObject.AddComponent<PoliceLights>(); lights.Siren = !quiet; lights.Target = null; driver.Lights = lights;
            var cop = new Cop { id = copSerial, car = car, driver = driver, lights = lights, bornAt = Time.time };
            driver.Place(at, forward);
            Cops.Add(cop); return cop;
        }
        // 0.97: every cop has the runner's vehicle's top speed and acceleration (the faster of two runners), and at least its grip and braking
        void MatchRunner(ArcadeVehicle car)
        {
            float top = 0, acc = 0, grip = 0, brake = 0, lat = 0;
            foreach (var r in Runners) { var m = r.car; if (!m) continue; top = Mathf.Max(top, m.topSpeed); acc = Mathf.Max(acc, m.acceleration); grip = Mathf.Max(grip, m.maxGripAcceleration); brake = Mathf.Max(brake, m.braking); lat = Mathf.Max(lat, m.lateralGrip); }
            if (top <= 0) return;
            car.topSpeed = top; car.acceleration = acc; car.maxGripAcceleration = Mathf.Max(car.maxGripAcceleration, grip); car.braking = Mathf.Max(car.braking, brake); car.lateralGrip = Mathf.Max(car.lateralGrip, lat);
        }
        void ClearCops()
        {
            if (Heli) { Destroy(Heli.gameObject); Heli = null; }
            foreach (var c in Cops) if (c.car) { c.car.gameObject.SetActive(false); Destroy(c.car.gameObject); }
            Cops.Clear(); Blocks.Clear();
        }
        public void Contact(CopDriver driver, Collision c)
        {
            var other = c.collider.GetComponentInParent<ArcadeVehicle>(); if (!other) return;
            foreach (var r in Runners)
                if (r.car == other)
                {
                    r.contactAt = Time.time;
                    if (Difficulty > 0 && State == Phase.Running && c.relativeVelocity.magnitude > 5 && Time.time - r.lastRam > 6 && r.body && driver.Car && RunnerRammed(r, driver)) { r.lastRam = Time.time; BumpHeat("the suspect rammed a unit"); }
                }
        }
        static bool RunnerRammed(Runner r, CopDriver cop)
        {
            var toCop = cop.Car.Body.position - r.body.position; toCop.y = 0; if (toCop.sqrMagnitude < .01f) return false; toCop.Normalize();
            return Vector3.Dot(r.prevVel, toCop) > Vector3.Dot(cop.PrevVel, -toCop);
        }
        void FixedUpdate() { foreach (var r in Runners) if (r.body) r.prevVel = r.body.linearVelocity; }

        // ---------- the round ----------
        void Update()
        {
            if (Runners.Count == 0 || Time.timeScale == 0) return;
            if (!PoliceKit.FreeRoam) { End(); return; }   // never in a race
            foreach (var car in held) if (car && (!car.Body.isKinematic || car.enabled)) { car.Body.isKinematic = true; car.enabled = false; }
            float t = Time.time - phaseAt;
            switch (State)
            {
                case Phase.Starting:
                    if (t >= RunnerRelease)
                    {
                        foreach (var r in Runners) if (held.Contains(r.car) && r.holdUntil == 0) PoliceKit.Hold(held, r.car, false, !r.human);
                        State = Phase.Running; foreach (var c in Cops) { c.driver.Frozen = false; c.lights.Siren = true; } foreach (var r in Runners) r.holdUntil = 0; Clock = 0;
                    }
                    break;
                case Phase.Running: RunningUpdate(); break;
                case Phase.Over:
                    if (t >= (Embedded ? EmbeddedEndPause : EndPause)) Finish();
                    break;
            }
            foreach (var r in Runners) if (r.holdUntil > 0 && Time.time >= r.holdUntil && !r.Done) { r.holdUntil = 0; PoliceKit.Hold(held, r.car, false, !r.human); }
        }
        void RunningUpdate()
        {
            float dt = Time.deltaTime; Clock += dt;
            foreach (var r in Runners)
            {
                if (r.Done) continue; r.freeSeconds = Clock;
                float speed = r.body.linearVelocity.magnitude; int near = 0;
                foreach (var c in Cops) { if (!c.car) continue; float d = Vector3.Distance(c.car.Body.position, r.body.position); if (d < CatchDistance) near++; }
                float rate = -1f / 3f;
                int pin = 0; foreach (var c in Cops) if (c.car && !c.block && Vector3.Distance(c.car.Body.position, r.body.position) < PinDistance) pin++;
                if (r.underground) rate = -1f / 3f;   // Round 39: they cannot reach you down there
                else if (near >= 1 && speed < CatchSpeed) rate = 1f / 3f;
                else if (near >= 2 && speed < BoxSpeed) rate = 1f / 6f;
                else if (near >= 1 && Time.time - r.contactAt < .6f && speed < ContactSpeed) rate = 1f / 9f;
                else if (pin >= 2 && speed < PinSpeed) rate = 1f / 6f;
                else if (near >= 1) rate = 1f / 12f;
                else if (speed < HeldSpeed && Cops.Any(c => c.car && !c.block && Vector3.Distance(c.car.Body.position, r.body.position) < HeldDistance)) rate = 1f / 6f;
                if (rate > 0) rate *= BustRate[Difficulty];
                r.bust = Mathf.Clamp01(r.bust + rate * dt);
                if (r.bust >= 1) Caught(r);
            }
            foreach (var r in Runners)
            {
                if (r.Done) continue;
                if (r.seen) r.escape = 0;
                else
                {
                    float nearest = float.MaxValue; foreach (var c in Cops) if (c.car && !c.block) nearest = Mathf.Min(nearest, Vector3.Distance(c.car.Body.position, r.body.position));
                    if (nearest >= NearCopsBy[Difficulty] || r.underground) r.escape = Mathf.Clamp01(r.escape + dt / EscapeBy[Difficulty] * (r.underground ? 2f : 1f));
                }
                if (r.escape >= 1) Escaped(r, false);
            }
            if (Time.time >= nextTick) { nextTick = Time.time + .2f; Tick(); }
            if (Runners.Any(r => !r.Done))
            {
                if (Runners.Any(r => !r.Done && r.copsChasing > 0)) heatClock += dt;
                if (heatClock >= HeatEvery[Difficulty] * (CheckFast ? .2f : 1f)) BumpHeat("more units on the way");
                foreach (var r in Runners) if (!r.Done && r.offRoad && r.seen && !r.offBumped && Difficulty > 0) { r.offSeen += dt; if (r.offSeen >= 10) { r.offBumped = true; BumpHeat("the suspect is running cross-country"); } }
                if (Heat >= 4 && !Heli) SpawnHelicopter();
            }
            if (Time.time >= nextBackup) { nextBackup = Time.time + 4; Backups(); }
            if (Heat >= 5 && heatFiveAt > 0 && !heatFiveSaid && Time.time - heatFiveAt > 25) { heatFiveSaid = true; Say("R12"); }
            if (Time.time >= nextLoud)
            {
                nextLoud = Time.time + Random.Range(28f, 45f); var lr = Runners.FirstOrDefault(x => !x.Done && x.human);
                if (lr != null && Cops.Any(c => c.car && !c.block && c.driver.Role == CopDriver.Task.Chase && Vector3.Distance(c.car.Body.position, lr.body.position) < 35)) { loudLast = loudLast == "R17" ? "R18" : "R17"; Say(loudLast); }
            }
            if (Heat >= 3 && Time.time >= nextBlock && Blocks.Count < 2) { nextBlock = Time.time + 4; TryRoadblock(); }   // Round 39: a try that finds no place comes again in 4 s (it waited 38)
            if (Time.time >= nextPrune) { nextPrune = Time.time + 2; Prune(); }
            UpdateBlocks();
            var humans = Runners.Where(r => r.human).ToList();
            bool anyEscapedFirst = humans.Any(r => r.escaped && !r.limit);
            if (State == Phase.Running)
            {
                if (humans.All(r => r.Done)) EndRound();
                else if (humans.Count > 1 && anyEscapedFirst) EndRound();
                else if (Clock >= Limit) { foreach (var r in Runners) if (!r.Done) Escaped(r, true); EndRound(); }
            }
        }
        void EndRound()
        {
            State = Phase.Over; phaseAt = Time.time;
            foreach (var r in Runners) if (!r.Done) Escaped(r, true);
            foreach (var c in Cops) c.driver.Role = CopDriver.Task.Idle;
        }
        void Finish()
        {
            if (Embedded) { FinishEmbedded(); return; }
            State = Phase.Done; foreach (var r in Runners) PoliceKit.Hold(held, r.car, false, !r.human);
            RecordTop10(); ClearCops(); Finished?.Invoke(this);
        }
        // Free Roam carries on: escaped, the cops leave; caught, the vehicle is set at the roadside, stopped. Nothing is recorded.
        void FinishEmbedded()
        {
            State = Phase.Done; var r = Runners.FirstOrDefault();
            if (r != null && r.caught) SetAtRoadside(r);
            if (r != null) PoliceKit.Hold(held, r.car, false);
            HiddenPolice.Ended(r != null && r.caught); End();
        }
        void SetAtRoadside(Runner r)
        {
            int node = Net.Nearest(r.body.position, out _, 400); if (node < 0) return;
            var t = Net.Tangent(node); var right = Vector3.Cross(Vector3.up, t).normalized; float side = Vector3.Dot(r.body.position - Net.P[node], right) >= 0 ? 1 : -1;
            var at = RaceRoute.Surface(Net.P[node].x + right.x * side * (Net.Half[node] + 2.6f), Net.P[node].z + right.z * side * (Net.Half[node] + 2.6f));
            PoliceKit.Put(r.car, at, Vector3.Dot(r.car.transform.forward, t) >= 0 ? t : -t);
        }
        public void Rematch() { BeginRound(); }
        void Caught(Runner r)
        {
            if (r.Done) return; r.caught = true; r.endedAt = Clock; r.freeSeconds = Clock; r.outcome = "CAUGHT"; PoliceKit.Hold(held, r.car, true, !r.human);
            if (r.human) Say(Embedded ? "H04" : "R03", r.player, true); else SayText("Suspect in custody");
            Banner = "BUSTED"; BannerAt = Time.unscaledTime;
            foreach (var c in Cops) if (c.assigned == r) c.assigned = null;
        }
        void Escaped(Runner r, bool limit)
        {
            if (r.Done) return;
            if (!limit && !r.human) { r.escape = 0; SayText("Unit lost the AI runner"); return; }   // the AI player 2 never ends the round
            r.escaped = true; r.limit = limit; r.endedAt = Clock; r.freeSeconds = Clock; r.outcome = "ESCAPED";
            Say(Embedded ? "H03" : limit ? "R05" : "R04", r.player, true);
            if (!limit && !Embedded) PoliceKit.Hold(held, r.car, true, !r.human);
            Banner = "YOU LOST THEM"; BannerAt = Time.unscaledTime;
        }

        // ---------- sight, roles, radio (5 times a second) ----------
        void Tick()
        {
            PeakCops = Mathf.Max(PeakCops, Cops.Count(c => c.car && !c.block));
            foreach (var r in Runners)
            {
                if (r.Done) { r.seen = false; continue; }
                // Round 39: in the pipe network nobody sees the runner (the cops do not follow down)
                bool under = PoliceKit.Underground(r.body.position);
                if (under != r.underground) { r.underground = under; if (r.human) { if (under) Say("R19", r.player, true); } }
                bool seen = false; int chasing = 0;
                foreach (var c in Cops)
                {
                    if (!c.car) continue; c.sees = false; if (under) continue; float d = Vector3.Distance(c.car.Body.position, r.body.position);
                    if (d <= SightBy[Difficulty] && PoliceKit.LineOfSight(c.car.Body.position + Vector3.up * 1.5f, r.body.position + Vector3.up * 1.1f, r.car.transform)) { c.sees = true; seen = true; }
                    if (c.sees || (c.assigned == r && c.driver.Role != CopDriver.Task.Idle && d < 220)) chasing++;
                }
                r.heliSees = !under && Heli && Heli.Sees(r, nextHeliCheck <= Time.time); if (r.heliSees) { seen = true; chasing++; }
                if (r.human && Heli && Time.time >= heliCalm) { if (r.heliSees && !heliSaw) { heliSaw = true; heliCalm = Time.time + 20; Say("U7", "R07"); } else if (!r.heliSees && heliSaw) { heliSaw = false; heliCalm = Time.time + 20; Say("R08"); } }
                bool was = r.seen; r.seen = seen; r.copsChasing = chasing; r.wasSeen |= seen;
                if (seen)
                {
                    bool upAgain = !was && r.lastSeenAt > 0 && Time.time - r.lastSeenAt > 3 && r.wasUnder;
                    r.lastSeenAt = Time.time; r.lastSeenPos = r.body.position; r.lastSeenVelocity = r.body.linearVelocity; r.unseenFor = 0;
                    if (upAgain && r.human) Say("R20", r.player, true);
                    else if (!was && Time.time - r.seenAt > 4 && r.seenAt > 0 && r.human) Say("R02");
                    r.seenAt = Time.time; r.wasUnder = false;
                    // the street they are on: turning onto one, and now and then which way they are heading on it
                    if (!r.offRoad && r.human)
                    {
                        int sn = Net.Nearest(r.body.position, out float sd, 40); string st = sn >= 0 && sd < Net.Half[sn] + 4f ? PoliceRadio.RoadId(Net, sn) : null;
                        if (st != null && st != r.street) { if (r.street != null) Say(new[] { "P15", st }, r.player); r.street = st; r.nextHeading = Time.time + 18; }
                        else if (st != null && Time.time >= r.nextHeading) { r.nextHeading = Time.time + Random.Range(22f, 34f); var hp = PoliceRadio.HeadingPhrase(r.body.linearVelocity); if (hp != null && r.body.linearVelocity.magnitude > 6) Say(new[] { hp, st }, r.player); }
                    }
                }
                else
                {
                    r.unseenFor += .2f; if (under) r.wasUnder = true;
                    if (was && r.human && !under) Say(PoliceRadio.With("P01", Place(r.lastSeenPos, r.lastSeenVelocity)), r.player);
                    if (r.human && r.wasSeen && r.unseenFor >= 1.6f && Time.time - r.lastCallIn > 14) { ComputeExits(r); CallIn(r); }
                }
                float off = under ? 0 : Net.OffNet(r.body.position);
                if (!r.offRoad && off > 14) { if (r.offSince < 0) r.offSince = Time.time; if (Time.time - r.offSince > 1.2f) GoOffRoad(r); }
                else if (!r.offRoad) r.offSince = -1;
                if (r.offRoad && off < 9) { if (r.onSince <= 0) r.onSince = Time.time; if (Time.time - r.onSince > 1f) BackOnRoad(r); }
                else if (r.offRoad) r.onSince = 0;
                if (!r.offRoad && !under) { int node = Net.Nearest(r.body.position, out _, 60); if (node >= 0) { r.offFrom = Net.P[node]; r.offFromNode = node; } }
                foreach (var c in Cops) { if (!c.car) continue; float d = Vector3.Distance(c.car.Body.position, r.body.position); if (d < 10) c.wasClose = true; else if (d > 26 && c.wasClose) { c.wasClose = false; r.dodged++; } }
            }
            AssignRoles();
            foreach (var c in Cops)
            {
                if (!c.car) continue; var r = c.assigned ?? Runners.FirstOrDefault(x => !x.Done); c.lights.Target = r != null ? r.car.transform : null;
                c.lights.Silent = r != null && Cops.Count(o => o != c && o.car && Vector3.Distance(o.car.Body.position, r.body.position) < Vector3.Distance(c.car.Body.position, r.body.position)) >= 2;
            }
            foreach (var r in Runners) r.topHeat = Mathf.Max(r.topHeat, Heat);
            if (nextHeliCheck <= Time.time) nextHeliCheck = Time.time + .6f;
            Boosts();
        }
        float nextHeliCheck;
        void Boosts()
        {
            foreach (var c in Cops)
            {
                if (!c.car || c.block) continue; c.boost = 1; var r = c.assigned; if (r == null || r.Done) { c.driver.Boost = 1; continue; }
                float d = Vector3.Distance(c.car.Body.position, r.body.position);
                if (d > 150) c.catching = true; else if (d < 80) c.catching = false;
                if (c.catching) c.boost = 1 + CatchUp[Difficulty] * Mathf.Clamp01((d - 80) / 220f);
                c.driver.Boost = c.boost;
            }
        }
        void GoOffRoad(Runner r)
        {
            r.offRoad = true; r.onSince = 0; r.offSeen = 0; r.offBumped = false;
            if (r.seen || Time.time - r.lastSeenAt < 6) { var v = r.body.linearVelocity.sqrMagnitude > 4 ? r.body.linearVelocity : r.car.transform.forward; Say(PoliceRadio.With("P02", PoliceRadio.PlaceId(Net, r.body.position, v)), r.player); }
            ComputeExits(r); if (r.human) CallIn(r);
        }
        void BackOnRoad(Runner r) { r.offRoad = false; r.offSince = -1; ExitPoints.Clear(); foreach (var c in Cops) c.exitUnit = false; if (r.seen || Time.time - r.lastSeenAt < 8) Say(PoliceRadio.With("P03", Place(r.body.position, Vector3.zero)), r.player); }
        void Say(string[] ids, int player = 0, bool priority = false)
        {
            string line = PoliceRadio.TextOf(ids); if (line == "") return;
            if (RadioLog.Count > 0 && RadioLog[RadioLog.Count - 1] == line && Time.unscaledTime - radioAt < 8) return;
            RadioLog.Add(line); if (RadioLog.Count > 30) RadioLog.RemoveAt(0); radioAt = Time.unscaledTime; PoliceRadio.Speak(ids, priority);
        }
        void Say(string id, int player = 0, bool priority = false) => Say(new[] { id }, player, priority);
        void Say(string a, string b) => Say(new[] { a, b });
        void SayText(string line) { if (RadioLog.Count > 0 && RadioLog[RadioLog.Count - 1] == line && Time.unscaledTime - radioAt < 8) return; RadioLog.Add(line); if (RadioLog.Count > 30) RadioLog.RemoveAt(0); radioAt = Time.unscaledTime; }
        string Place(Vector3 p, Vector3 v) => PoliceRadio.PlaceId(Net, p, v);
        static string UnitId(int serial) => "U" + (((serial - 1) % 6) + 1);
        float heatFiveAt = -1, nextLoud = 20, heliCalm; bool heliSaw, heatFiveSaid; string loudLast = "R18";

        readonly List<int> exitNodes = new();
        void ComputeExits(Runner r)
        {
            exitNodes.Clear(); ExitPoints.Clear(); var pos = r.body.position; var heading = r.body.linearVelocity; heading.y = 0; bool moving = heading.sqrMagnitude > 9; if (moving) heading.Normalize();
            var cands = new List<(float score, int node)>(); int here = Net.Nearest(pos, out _, 900, 40); if (here < 0) return; int comp = Net.Component[here];
            foreach (int i in Net.Near(pos, 900))
            {
                if (Net.Component[i] != comp) continue;
                var d = Net.P[i] - pos; d.y = 0; float dist = d.magnitude; if (dist < 120) continue;
                float cone = moving ? Vector3.Angle(heading, d) : 0; if (cone > 80) continue;
                float junction = Net.Adj[i].Count >= 3 ? -60f : 0;
                cands.Add((dist + cone * 2.5f + junction, i));
            }
            foreach (var cand in cands.OrderBy(c => c.score))
            {
                if (exitNodes.Count >= 4) break;
                if (exitNodes.Any(n => Vector3.Distance(Net.P[n], Net.P[cand.node]) < 90)) continue;
                exitNodes.Add(cand.node);
            }
            foreach (int n in exitNodes) ExitPoints.Add(Net.P[n]);
        }
        void CallIn(Runner r)
        {
            r.lastCallIn = Time.time; if (exitNodes.Count == 0) { Trace($"{Clock:F0}s call-in: no exits ahead"); return; }
            int want = Mathf.Min(exitNodes.Count, Mathf.Clamp(2 + (Heat >= 3 ? 1 : 0) + (Heat >= 5 ? 1 : 0), 2, 4)); int placed = 0; var names = new List<string>();
            foreach (int node in exitNodes.OrderBy(n => Vector3.Distance(Net.P[n], r.body.position)))
            {
                if (placed >= want) break;
                if (Cops.Count(c => c.car && !c.block) >= MaxCops + 4) break;
                if (Cops.Any(c => c.car && !c.block && Vector3.Distance(c.car.Body.position, Net.P[node]) < 45)) { placed++; continue; }
                int at = node; if (PoliceKit.Seen(Net.P[at])) { at = HiddenNear(node); if (at < 0) continue; }
                var face = r.body.position - Net.P[at]; face.y = 0; var t = Net.Tangent(at); if (Vector3.Dot(t, face) < 0) t = -t;
                var edge = Vector3.Cross(Vector3.up, t).normalized * Mathf.Max(0, Net.Half[at] - 1.6f) * (Random.value < .5f ? -1 : 1);
                var cop = SpawnCop(Net.P[at] + edge, t); if (cop == null) continue;
                cop.exitUnit = true; cop.driver.Frozen = false; cop.driver.Role = CopDriver.Task.Exit; cop.driver.Hold = true; cop.lights.Siren = true; cop.lights.Silent = false; placed++;
                var st = PoliceRadio.RoadId(Net, at); if (st != null) names.Add(st);
            }
            Trace($"{Clock:F0}s call-in: exits {exitNodes.Count}, wanted {want}, placed {names.Count}, cops now {Cops.Count(c => c.car && !c.block)}");
            var named = names.Distinct().Take(2).ToList();
            if (named.Count > 0 && r.human) Say(named.Count == 1 ? new[] { "P04", named[0] } : new[] { "P04", named[0], "P05", named[1] }, r.player);
            else if (placed > 0 && r.human) Say("R10", r.player);
        }
        float nextExitRefresh;
        void AssignRoles()
        {
            foreach (var c in Cops) { if (!c.car || c.block) continue; Runner best = null; float bd = float.MaxValue; foreach (var r in Runners) { if (r.Done) continue; float d = Vector3.Distance(c.car.Body.position, r.body.position); if (d < bd) { bd = d; best = r; } } c.assigned = best; if (best == null) { c.driver.Role = CopDriver.Task.Idle; c.driver.Stop(); } }
            foreach (var r in Runners)
            {
                if (r.Done) continue;
                var mine = Cops.Where(c => c.car && !c.block && c.assigned == r).OrderBy(c => Vector3.Distance(c.car.Body.position, r.body.position)).ToList(); if (mine.Count == 0) continue;
                bool known = !r.underground && (r.seen || Time.time - r.lastSeenAt < 3);
                if (r.offRoad && !r.underground && (r.seen || Time.time - r.lastSeenAt < 40 || !r.human)) OffRoadRoles(r, mine);
                else if (known) ChaseRoles(r, mine);
                else SearchRoles(r, mine);
            }
        }
        void Plan(Cop c, int node, CopDriver.Task role, bool hold, float every = 1.2f)
        {
            c.driver.Role = role; c.driver.Hold = hold;
            if (node < 0) return;
            bool force = c.driver.GoalNode < 0 || c.driver.Arrived && !hold;
            bool far = c.driver.GoalNode < 0 || Vector3.Distance(Net.P[node], Net.P[c.driver.GoalNode]) > 30;
            if (Time.time < c.nextPlan && !force && !far) return;
            c.nextPlan = Time.time + every + Random.value * .4f; if (far || force || Time.time >= c.nextPlan - every) c.driver.GoTo(node);
        }
        void ChaseRoles(Runner r, List<Cop> mine)
        {
            var heading = r.body.linearVelocity; heading.y = 0; if (heading.sqrMagnitude < 4) heading = r.car.transform.forward;
            int runnerNode = Net.Nearest(r.body.position + heading.normalized * Mathf.Min(12, heading.magnitude * .5f), out _, 120);
            for (int i = 0; i < mine.Count; i++)
            {
                var c = mine[i]; c.driver.Chasing = null; c.driver.ChasingBody = null; c.driver.Pushy = i < 2; c.driver.Catchup = 1;
                float d = Vector3.Distance(c.car.Body.position, r.body.position);
                if (i == 0 || (i == 1 && d < 160))
                {
                    c.driver.Chasing = r.car.transform; c.driver.ChasingBody = r.body; c.driver.Offset = i == 0 ? 0 : (Vector3.Dot(Vector3.Cross(Vector3.up, heading.normalized), c.car.Body.position - r.body.position) > 0 ? 3.4f : -3.4f);
                    Plan(c, runnerNode, i == 0 ? CopDriver.Task.Chase : CopDriver.Task.Flank, false, .8f);
                }
                else
                {
                    float ahead = Mathf.Clamp(heading.magnitude * 9 + 60 * (i - 1), 160, 520);
                    int cut = runnerNode >= 0 ? Net.Ahead(runnerNode, heading, ahead) : -1; c.driver.Pushy = false;
                    Plan(c, cut, CopDriver.Task.Cutoff, true, 2.5f);
                }
            }
        }
        void SearchRoles(Runner r, List<Cop> mine)
        {
            int last = Net.Nearest(r.lastSeenPos == Vector3.zero ? r.body.position : r.lastSeenPos, out _, 600, 40);
            float radius = Mathf.Clamp(110 + 22 * r.unseenFor, 110, 700);
            for (int i = 0; i < mine.Count; i++)
            {
                var c = mine[i]; c.driver.Chasing = null; c.driver.Pushy = false; c.driver.Catchup = 1;
                if (i <= 1 && r.unseenFor < 45 && last >= 0)
                {
                    var v = r.lastSeenVelocity; float ahead = Mathf.Clamp(v.magnitude * (r.unseenFor + 4) * (i == 0 ? .8f : 1.2f), 40, 900);
                    int guess = Net.Ahead(last, v, ahead); if (i == 0 && !r.offRoad && !r.underground && r.unseenFor < 45) { int on = Net.Nearest(r.body.position, out _, 300, 40); if (on >= 0) guess = on; }
                    Plan(c, guess >= 0 ? guess : last, CopDriver.Task.Search, false, 1.5f); continue;
                }
                if (i == 0) { Plan(c, last, CopDriver.Task.Search, false, 2f); continue; }
                if (Time.time >= c.searchUntil || c.driver.Arrived || c.driver.GoalNode < 0)
                {
                    var around = last >= 0 ? Net.Around(last, radius, 70) : null;
                    if (around != null && around.Count > 0) { int pick = around[Random.Range(0, around.Count)]; c.driver.Role = CopDriver.Task.Search; c.driver.Hold = false; c.driver.GoTo(pick); }
                    c.searchUntil = Time.time + 14 + Random.value * 6;
                }
                c.driver.Role = CopDriver.Task.Search;
            }
        }
        void OffRoadRoles(Runner r, List<Cop> mine)
        {
            if (Time.time >= nextExitRefresh) { nextExitRefresh = Time.time + 4; ComputeExits(r); if (r.human && Time.time - r.lastCallIn > 10) CallIn(r); }
            var free = new List<Cop>(mine); var edgeCandidates = free.Where(c => !c.exitUnit).ToList(); var edge = edgeCandidates.Count > 0 ? edgeCandidates[0] : free[0]; free.Remove(edge); edge.driver.Chasing = null; edge.driver.Pushy = false;
            Plan(edge, r.offFromNode, CopDriver.Task.Edge, true, 2f);
            var taken = new HashSet<Cop>();
            foreach (int node in exitNodes.OrderBy(n => Vector3.Distance(Net.P[n], r.body.position)))
            {
                Cop best = null; float bd = float.MaxValue;
                foreach (var c in free) { if (taken.Contains(c)) continue; float d = Vector3.Distance(c.car.Body.position, Net.P[node]); if (d < bd) { bd = d; best = c; } }
                if (best == null) break; taken.Add(best); best.driver.Chasing = null; best.driver.Pushy = false; best.driver.Catchup = 1; Plan(best, node, CopDriver.Task.Exit, true, 3f);
            }
            foreach (var c in free) if (!taken.Contains(c)) { c.driver.Chasing = null; SearchAround(c, r); }
        }
        void SearchAround(Cop c, Runner r)
        {
            if (Time.time < c.searchUntil && !c.driver.Arrived && c.driver.GoalNode >= 0) return;
            int centre = Net.Nearest(r.lastSeenPos == Vector3.zero ? r.body.position : r.lastSeenPos, out _, 600, 40); var around = centre >= 0 ? Net.Around(centre, 400, 70) : null;
            if (around != null && around.Count > 0) { c.driver.Role = CopDriver.Task.Search; c.driver.Hold = false; c.driver.GoTo(around[Random.Range(0, around.Count)]); }
            c.searchUntil = Time.time + 12;
        }

        // ---------- backup and roadblocks ----------
        int NearCount(Runner r) => Cops.Count(c => c.car && !c.block && Vector3.Distance(c.car.Body.position, r.body.position) <= 600);
        void Backups()
        {
            Runner r = null; int fewest = int.MaxValue;
            foreach (var x in Runners) { if (x.Done || !x.human) continue; int n = NearCount(x); if (n < fewest) { fewest = n; r = x; } }
            if (r == null) return;
            int want = Mathf.Min(MaxCops, Heat + 1);
            if (fewest < want && Cops.Count(c => c.car && !c.block) < MaxCops + 4)
            {
                int node = PlaceAhead(r); if (node < 0) return;
                var cop = SpawnCop(Net.P[node], FaceRunner(node, r)); if (cop != null) { cop.driver.Frozen = false; cop.catching = false; var st = PoliceRadio.RoadId(Net, node); Say(st != null ? new[] { UnitId(cop.id), "P08", st } : new[] { "R13" }, r.player); }
                return;
            }
            foreach (var c in Cops)
            {
                if (!c.car || c.block || c.exitUnit || c.driver.Role == CopDriver.Task.Exit || c.sees) { if (c.sees) c.farSince = -1; continue; }
                var cr = c.assigned ?? r; if (cr == null) continue; var heading = cr.body.linearVelocity; heading.y = 0; if (heading.sqrMagnitude < 4) heading = cr.car.transform.forward;
                var to = c.car.Body.position - cr.body.position; to.y = 0;
                bool behind = Vector3.Dot(to, heading) < 0 && to.magnitude > 600;
                if (!behind) { c.farSince = -1; continue; }
                if (c.farSince < 0) c.farSince = Time.time; if (Time.time - c.farSince < 3) continue;
                if (PoliceKit.Seen(c.car.Body.position)) continue;
                int node = PlaceAhead(cr); if (node < 0) continue;
                c.driver.Place(Net.P[node], FaceRunner(node, cr)); c.driver.Stop(); c.farSince = -1; c.catching = false;
                var st2 = PoliceRadio.RoadId(Net, node); if (st2 != null) Say(new[] { UnitId(c.id), "P09", st2 }, cr.player);
                break;
            }
        }
        Vector3 FaceRunner(int node, Runner r) { var face = r.body.position - Net.P[node]; face.y = 0; var t = Net.Tangent(node); return Vector3.Dot(t, face) < 0 ? -t : t; }
        public int PlaceAhead(Runner r)
        {
            var heading = r.body.linearVelocity; heading.y = 0; if (heading.sqrMagnitude < 9) heading = r.car.transform.forward; heading.y = 0; heading.Normalize();
            int centre = r.offRoad && r.offFromNode >= 0 ? r.offFromNode : Net.Nearest(r.body.position, out _, 400, 40); if (centre < 0) return -1;
            int route = Net.Ahead(centre, heading, 380);
            var best = new List<(float score, int node)>();
            foreach (int i in Net.Near(r.body.position, 450))
            {
                if (Net.Component[i] != Net.Component[centre]) continue; var d = Net.P[i] - r.body.position; d.y = 0; float dist = d.magnitude; if (dist < 250) continue;
                float ahead = Vector3.Dot(d / dist, heading); if (ahead < -.05f) continue;
                if (PoliceKit.Seen(Net.P[i]) && dist < 380) continue;
                if (Cops.Any(c => c.car && Vector3.Distance(c.car.Body.position, Net.P[i]) < 70)) continue;
                float score = ahead * 60 + Mathf.Max(0, 120 - Vector3.Distance(Net.P[i], Net.P[route])) * .6f - Mathf.Abs(dist - 350) * .15f;
                best.Add((score, i));
            }
            if (best.Count == 0) return -1;
            var top = best.OrderByDescending(x => x.score).Take(5).ToList(); return top[Random.Range(0, top.Count)].node;
        }
        void Prune()
        {
            var live = Cops.Where(c => c.car && !c.block).ToList(); if (live.Count <= MaxCops + 2) return;
            var r = Runners.FirstOrDefault(x => !x.Done && x.human); if (r == null) return;
            foreach (var c in live.OrderByDescending(c => Vector3.Distance(c.car.Body.position, r.body.position)))
            {
                if (c.sees || PoliceKit.Seen(c.car.Body.position) || Vector3.Distance(c.car.Body.position, r.body.position) < 200) continue;
                Cops.Remove(c); c.car.gameObject.SetActive(false); Destroy(c.car.gameObject); break;
            }
        }
        public void BumpHeat(string why)
        {
            heatClock = 0; if (Heat >= 5) return;
            Heat++; foreach (var r in Runners) r.topHeat = Mathf.Max(r.topHeat, Heat);
            string[] says = { "", "", "backup requested", "roadblocks and more units", "air support inbound", "every unit responding" };
            Alert = $"HEAT {Heat} — {says[Heat]}"; AlertAt = Time.unscaledTime; radioAt = -99;
            if (why.Contains("rammed")) Say("R11", 0, true); else if (why.Contains("cross-country")) Say("R16", 0, true);
            else if (Heat == 2) Say("R13", 0, true); else if (Heat == 3) Say("R14", 0, true); else if (Heat == 5) { Say("R15", 0, true); heatFiveAt = Time.time; }
        }
        void SpawnHelicopter()
        {
            var r = Runners.FirstOrDefault(x => !x.Done && x.human); if (r == null) return;
            var go = new GameObject("Police helicopter"); Heli = go.AddComponent<PoliceHelicopter>(); Heli.Begin(this, r);
            Say("R06", r.player, true);
        }
        int HiddenNear(int node)
        {
            int best = -1; float bd = float.MaxValue;
            foreach (int i in Net.Near(Net.P[node], 90)) { if (Net.Component[i] != Net.Component[node]) continue; float d = Vector3.Distance(Net.P[i], Net.P[node]); if (d < bd && !PoliceKit.Seen(Net.P[i])) { bd = d; best = i; } }
            return best;
        }
        void TryRoadblock()
        {
            var r = Runners.FirstOrDefault(x => !x.Done && x.human && !x.offRoad && !x.underground); if (r == null) return;
            var heading = r.body.linearVelocity; heading.y = 0; if (heading.magnitude < 6) return; heading.Normalize();
            int here = Net.Nearest(r.body.position, out float off, 80); if (here < 0 || off > 20) return;
            float distance = Mathf.Clamp(r.body.linearVelocity.magnitude * 11f, 240, 420);
            int node = Net.Ahead(here, heading, distance); if (node < 0 || node == here) return;
            if (Vector3.Distance(Net.P[node], r.body.position) < 180) return;
            if (PoliceKit.Seen(Net.P[node])) { nextBlock = Time.time + 4; return; }
            var tangent = Net.Tangent(node); var right = Vector3.Cross(Vector3.up, tangent).normalized; float half = Net.Half[node];
            float gap = 2.7f, centreLane = Random.Range(-Mathf.Max(0, half - gap - .8f), Mathf.Max(0, half - gap - .8f));
            var block = new Roadblock { centre = Net.P[node], tangent = tangent, born = Time.time, target = r, node = node };
            for (int side = -1; side <= 1; side += 2)
            {
                float lateral = centreLane + side * (gap + 2.3f); var at = Net.P[node] + right * lateral;
                var facing = Quaternion.LookRotation(Quaternion.Euler(0, 90 + Random.Range(-14f, 14f), 0) * tangent);
                var cop = SpawnCop(at, facing * Vector3.forward, false, false); if (cop == null) continue;
                cop.block = true; cop.driver.Role = CopDriver.Task.Block; cop.driver.Frozen = true; cop.car.Body.isKinematic = true; cop.lights.Siren = true; cop.lights.Silent = true;
                if (side < 0) block.a = cop; else block.b = cop;
            }
            Blocks.Add(block); nextBlock = Time.time + 38; Say(PoliceRadio.With("P06", PoliceRadio.RoadId(Net, node), "P07"));
        }
        void UpdateBlocks()
        {
            for (int i = Blocks.Count - 1; i >= 0; i--)
            {
                var b = Blocks[i]; var r = b.target; bool remove = Time.time - b.born > 80 || r == null || r.Done;
                if (!remove && r != null && Vector3.Distance(r.body.position, b.centre) > 80) foreach (var c in Cops) if (c.car && !c.block && Vector3.Distance(c.car.Body.position, b.centre) < 45) { remove = true; break; }
                if (r != null && !r.Done)
                {
                    float d = Vector3.Distance(r.body.position, b.centre); b.closest = Mathf.Min(b.closest, d);
                    if (b.closest < 22 && d > 45 && !b.passed && r.bust < .9f) { b.passed = true; r.blocks++; Say("R09"); if (Difficulty > 0) BumpHeat("the suspect ran the roadblock"); }
                    if (b.passed && d > 120) remove = true;
                    if (!remove && d > 420 && Time.time - b.born > 25) remove = true;
                }
                if (!remove) continue;
                foreach (var c in new[] { b.a, b.b }) if (c != null) { Cops.Remove(c); if (c.car) { c.car.gameObject.SetActive(false); Destroy(c.car.gameObject); } }
                Blocks.RemoveAt(i);
            }
        }

        // ---------- results, Top 10 (getaway-v1.json beside the race records, by round length) ----------
        [System.Serializable] public sealed class Entry { public string name; public float seconds; public int heat, minutes; public string outcome, date; }
        [System.Serializable] public sealed class Board { public List<Entry> entries = new(); }
        public const string BoardFile = "getaway-v1.json";
        public readonly List<int> Places = new();
        static IEnumerable<Entry> Sorted(Board b, int minutes) => b.entries.Where(x => x.minutes == minutes).OrderByDescending(x => x.seconds).ThenByDescending(x => x.heat);
        public static List<Entry> Top(int minutes) => Sorted(PoliceKit.Load<Board>(BoardFile), minutes).Take(10).ToList();
        void RecordTop10()
        {
            Places.Clear(); var board = PoliceKit.Load<Board>(BoardFile);
            foreach (var r in Runners.Where(x => x.human))
            {
                var e = new Entry { name = PoliceKit.NameOf(r.player), seconds = Mathf.Round(r.freeSeconds * 10) / 10f, heat = r.topHeat, minutes = PoliceSetup.Minutes, outcome = r.outcome, date = System.DateTime.Now.ToString("yyyy-MM-dd") };
                board.entries.Add(e); var top = Sorted(board, e.minutes).Take(10).ToList(); Places.Add(top.IndexOf(e) + 1);
                board.entries = board.entries.Where(x => x.minutes != e.minutes || top.Contains(x)).ToList();
            }
            PoliceKit.Save(BoardFile, board);
        }
        public int Winner
        {
            get
            {
                var humans = Runners.Where(r => r.human).ToList(); if (humans.Count < 2) return 0;
                var esc = humans.Where(r => r.escaped && !r.limit).OrderBy(r => r.endedAt).FirstOrDefault(); if (esc != null) return esc.player;
                var a = humans[0]; var b = humans[1];
                if (a.caught != b.caught) return a.caught ? b.player : a.player;
                if (Mathf.Approximately(a.freeSeconds, b.freeSeconds)) return 0;
                return a.freeSeconds > b.freeSeconds ? a.player : b.player;
            }
        }
        public string Verdict(Runner r) => r.caught ? "CAUGHT after " + Mss(r.freeSeconds) : r.limit ? "ESCAPED (time up)" : "ESCAPED after " + Mss(r.freeSeconds);
        public IEnumerable<(Vector3 position, bool sees)> CopMarks => Cops.Where(c => c.car).Select(c => (c.car.Body.position, c.sees));
        public Vector3? HeliMark => Heli ? Heli.transform.position : null;
        public string DebugText
        {
            get
            {
                var sb = new System.Text.StringBuilder(); var r = Runners.FirstOrDefault(x => x.human) ?? Runners.FirstOrDefault(); if (r == null) return "";
                sb.AppendLine($"GETAWAY t {Clock:F0}s heat {Heat} cops {Cops.Count(c => !c.block)} (near {NearCount(r)}) seen {r.seen} heli {(Heli ? (r.heliSees ? "SEES" : "up") : "-")} escape {r.escape:F2} bust {r.bust:F2} off {r.offRoad} under {r.underground}");
                foreach (var c in Cops) { if (!c.car) continue; float d = Vector3.Distance(c.car.Body.position, r.body.position); sb.AppendLine($" #{c.id} {c.driver.Role}{(c.exitUnit ? "(exit)" : "")}{(c.block ? "(block)" : "")} {d:F0}m {c.car.Body.linearVelocity.magnitude:F0}m/s x{c.boost:F2}{(c.sees ? " sees" : "")}"); }
                return sb.ToString();
            }
        }
        public string HeatBars => new string('★', Heat) + new string('☆', 5 - Heat);
    }
}
