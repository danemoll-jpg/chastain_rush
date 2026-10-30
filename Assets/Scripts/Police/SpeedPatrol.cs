using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): Racer's Speed Patrol (0.95 Part G) in Free Roam on this map. Everyone is a cop (player 2's patrol car dark blue).
    // Each road's limit is PoliceRoads' (OSM's maxspeed, else the class's); ordinary traffic keeps to it while the round runs (TrafficDriver.
    // SpeedOverride); now and then a traffic car near a cop starts speeding (10 to 40 mph over; one every 20-30 s per cop, at most three). The radar
    // shows the speed of the vehicle the patrol car points at within 80 m (red over the limit) and clocks a speeder; lights on (RB / H) within 30 m
    // of a clocked speeder starts the pursuit (it speeds up harder); the pull-over meter fills while a cop with lights on stays within 20 m (4 s for a
    // slow speeder to 8 s for the fastest); full = caught (it pulls over). 60 s after it was clocked an uncaught speeder gets away. Points: a catch
    // 100 + 10 per mph over (half if rammed to a stop); hitting other traffic -50; lights on a car that was not speeding (within 20 m for 4 s) -25.
    // Traffic is always on (Light at least). Results and the Speed Patrol Top 10 per round length (speed-patrol-v1.json beside the race records).
    // (Racer's speeders also turned back when a cop got in front; this map's traffic drives a road graph one way at a time, so a speeder runs on.)
    public sealed class SpeedPatrol : MonoBehaviour
    {
        public const float RadarRange = 80, RadarCone = 10, PursuitRange = 30, MeterRange = 20, GetAwaySeconds = 60, NoViolationSeconds = 4, Overtime = 15, ResetHold = 2, StartPause = 3;
        public const int MaxSpeeders = 3, CatchPoints = 100, PerMph = 10, HitPenalty = 50, NoViolationPenalty = 25;
        public static SpeedPatrol Current { get; private set; }
        public static System.Action<SpeedPatrol> Finished;
        public enum Phase { Starting, Running, Overtime, Done }
        public sealed class Cop
        {
            public int player; public ArcadeVehicle car; public bool siren; public PoliceLights lights; public string wasVehicle; public int wasColour = -1;
            public int points, catches, penalties, hits, noViolations; public float fastest;
            public string line = ""; public float lineUntil; public float radar = -1; public bool radarOver; public int limit = -1, radarLimit = -1;
            public float nextSpawn, holdUntil; public readonly Dictionary<TrafficDriver, float> lightsOn = new(); public readonly HashSet<TrafficDriver> penalised = new(); public readonly Dictionary<ArcadeVehicle, float> lastHit = new();
            public readonly List<string> log = new(); public System.Action OnReset;
        }
        public sealed class Speeder
        {
            public TrafficDriver driver; public int over; public float since; public bool clocked; public float clockedAt; public int clockedOver; public bool pursuit;
            public readonly float[] meter = new float[3]; public float stoppedUntil = -1; public bool caught, gone; public float rammedAt = -1; public int rammedBy;
            public ArcadeVehicle Car => driver ? driver.Car : null;
        }
        public readonly List<Cop> Cops = new(); public readonly List<Speeder> Speeders = new();
        public Phase State { get; private set; }
        public float Clock { get; private set; }
        public float Limit => PoliceSetup.Minutes * 60;
        public static float TestLimit = -1; float RoundLimit => TestLimit > 0 ? TestLimit : Limit;   // checks only
        public int Clocked => Speeders.Count(s => s.clocked); public int GotAway { get; private set; }
        float phaseAt, overtimeAt, nextTraffic; int trafficWas = -2;
        readonly HashSet<ArcadeVehicle> held = new();
        public bool Holds(ArcadeVehicle car) => held.Contains(car);
        public static readonly Color SecondLivery = new(.04f, .09f, .32f);
        public Cop CopOf(int player) => Cops.FirstOrDefault(c => c.player == player);

        public static SpeedPatrol Begin() { End(); var p = new GameObject("Speed Patrol").AddComponent<SpeedPatrol>(); Current = p; p.BeginRound(); return p; }
        public static void End() { if (Current) { Destroy(Current.gameObject); Current = null; } }
        void OnDestroy()
        {
            foreach (var s in Speeders.ToList()) Release(s); foreach (var t in Traffic) t.SpeedOverride = -1;
            foreach (var c in Cops) { PoliceKit.Hold(held, c.car, false); if (c.lights) Destroy(c.lights); var pc = c.car ? c.car.GetComponent<PatrolContacts>() : null; if (pc) Destroy(pc); var rs = c.car ? c.car.GetComponent<VehicleRespawn>() : null; if (rs && c.OnReset != null) rs.Respawned -= c.OnReset; PoliceKit.Unwear(c.car, c.wasVehicle, c.wasColour); }
            if (trafficWas > -2) global::ChastainRush.Traffic.ForcedLevel = trafficWas;
            if (Current == this) Current = null; PoliceRadio.Clear();
        }
        public void BeginRound()
        {
            foreach (var s in Speeders.ToList()) Release(s); Speeders.Clear();
            foreach (var c in Cops) PoliceKit.Unwear(c.car, c.wasVehicle, c.wasColour); Cops.Clear(); GotAway = 0;
            // traffic is always on: at least Light while the round runs
            var tr = global::ChastainRush.Traffic.Instance; if (tr && tr.Level == 0) { trafficWas = global::ChastainRush.Traffic.ForcedLevel; global::ChastainRush.Traffic.ForcedLevel = 1; }
            AddCop(1, PoliceKit.Player(1)); if (SplitScreen.Active && SplitScreen.P2Car && !SplitScreen.P2Ai) AddCop(2, SplitScreen.P2Car);
            Clock = 0; State = Phase.Starting; phaseAt = Time.time; nextTraffic = 0;
            if (Cops.Count > 0) Say(Cops[0], "S01");
            foreach (var c in Cops) { PoliceKit.Hold(held, c.car, true); c.nextSpawn = Time.time + StartPause + Random.Range(5f, 9f) + (c.player - 1) * 4; }
        }
        void AddCop(int player, ArcadeVehicle car)
        {
            if (!car) return;
            var cop = new Cop { player = player, car = car };
            PoliceKit.Wear(car, PoliceSetup.CopVehicle, player == 1 ? new Color(.03f, .03f, .035f) : SecondLivery, out cop.wasVehicle, out cop.wasColour);
            cop.lights = car.GetComponent<PoliceLights>(); if (!cop.lights) cop.lights = car.gameObject.AddComponent<PoliceLights>(); cop.lights.Siren = false; cop.lights.Target = null; cop.lights.Rebuild();
            var contacts = car.GetComponent<PatrolContacts>(); if (!contacts) contacts = car.gameObject.AddComponent<PatrolContacts>(); contacts.Cop = cop; contacts.Patrol = this;
            var respawn = car.GetComponent<VehicleRespawn>(); cop.OnReset = () => { if (State == Phase.Running || State == Phase.Overtime) { cop.holdUntil = Time.time + ResetHold; PoliceKit.Hold(held, car, true); } }; if (respawn) respawn.Respawned += cop.OnReset;
            Cops.Add(cop);
        }
        IEnumerable<TrafficDriver> Traffic => global::ChastainRush.Traffic.Instance ? global::ChastainRush.Traffic.Instance.Cars.Where(d => d && d.Car && d.isActiveAndEnabled && d.gameObject.activeInHierarchy) : Enumerable.Empty<TrafficDriver>();
        Speeder SpeederOf(TrafficDriver d) => Speeders.FirstOrDefault(s => s.driver == d && !s.gone && !s.caught);
        Speeder SpeederOf(ArcadeVehicle car) => Speeders.FirstOrDefault(s => s.Car == car && !s.gone);

        void Update()
        {
            if (State == Phase.Done || Time.timeScale == 0) return;
            if (!PoliceKit.FreeRoam) { End(); return; }
            foreach (var car in held) if (car && (!car.Body.isKinematic || car.enabled)) { car.Body.isKinematic = true; car.enabled = false; }
            if (State == Phase.Starting) { if (Time.time - phaseAt >= StartPause) { foreach (var c in Cops) PoliceKit.Hold(held, c.car, false); State = Phase.Running; } return; }
            Clock += Time.deltaTime;
            foreach (var c in Cops)
            {
                if (c.holdUntil > 0 && Time.time >= c.holdUntil) { c.holdUntil = 0; PoliceKit.Hold(held, c.car, false); }
                if (Toggle(c.player)) c.siren = !c.siren;
                c.lights.Siren = c.siren; c.limit = PoliceKit.LimitAt(c.car.Body.position, out bool on); if (!on) c.limit = -1;
                Radar(c); Spawn(c); LightsOnTraffic(c);
            }
            foreach (var s in Speeders.ToList()) Run(s);
            if (Time.time >= nextTraffic) { nextTraffic = Time.time + .5f; Pace(); }
            if (State == Phase.Running && Clock >= RoundLimit) { State = Phase.Overtime; overtimeAt = Time.time; foreach (var c in Cops) Say(c, "S08"); }
            if (State == Phase.Overtime && (!Speeders.Any(s => s.pursuit && !s.caught && !s.gone) || Time.time - overtimeAt >= Overtime)) Finish();
        }
        public static bool ScriptedToggle1, ScriptedToggle2;   // checks: the lights button
        bool Toggle(int player)
        {
            if (player == 1 && ScriptedToggle1) { ScriptedToggle1 = false; return true; } if (player == 2 && ScriptedToggle2) { ScriptedToggle2 = false; return true; }
            var device = SplitScreen.Active ? (player == 1 ? SplitScreen.P1Device : SplitScreen.P2Device) : null;
            if (device is Gamepad g) return g.rightShoulder.wasPressedThisFrame;
            if (device is Keyboard k) return k.hKey.wasPressedThisFrame;
            if (!SplitScreen.Active && player == 1) return (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame);
            return false;
        }
        void Pace()
        {
            foreach (var d in Traffic)
            {
                var s = SpeederOf(d); int limit = PoliceKit.LimitAt(d.Car.Body.position, out _);
                if (s != null && s.stoppedUntil > 0) { d.SpeedOverride = 0; continue; }
                d.SpeedOverride = s == null ? PoliceKit.Mps(limit) * .97f : PoliceKit.Mps(limit + s.over + (s.pursuit ? 8 + s.over * .4f : 0));
            }
            foreach (var s in Speeders.Where(s => s.caught && s.stoppedUntil > 0 && Time.time >= s.stoppedUntil).ToList()) Release(s);
        }
        void Release(Speeder s) { if (s.driver) s.driver.SpeedOverride = -1; s.stoppedUntil = -1; Speeders.Remove(s); }
        void Spawn(Cop c)
        {
            if (State != Phase.Running || Time.time < c.nextSpawn) return;
            c.nextSpawn = Time.time + Random.Range(20f, 30f);
            if (Speeders.Count(s => !s.gone && !s.caught) >= MaxSpeeders) return;
            var pick = Traffic.Where(d => SpeederOf(d) == null && Speeders.All(s => s.driver != d)).Select(d => (d, dist: Vector3.Distance(d.Car.Body.position, c.car.Body.position))).Where(x => x.dist > 50 && x.dist < 260).OrderBy(x => x.dist).FirstOrDefault();
            if (!pick.d) { c.nextSpawn = Time.time + 4; return; }
            Speeders.Add(new Speeder { driver = pick.d, over = Random.Range(10, 41), since = Time.time });
        }
        // checks: a speeder now, the traffic car nearest ahead of this cop
        public bool ForceSpeeder(int player, int over)
        {
            var c = CopOf(player); if (c == null) return false;
            var d = Traffic.Where(t => SpeederOf(t) == null).OrderBy(t => Vector3.Distance(t.Car.Body.position, c.car.Body.position)).FirstOrDefault(); if (!d) return false;
            Speeders.Add(new Speeder { driver = d, over = over, since = Time.time }); return true;
        }
        void Radar(Cop c)
        {
            var f = Vector3.ProjectOnPlane(c.car.transform.forward, Vector3.up).normalized; TrafficDriver target = null; float best = RadarRange;
            foreach (var d in Traffic)
            {
                var to = d.Car.Body.position - c.car.Body.position; float dist = to.magnitude; if (dist > best || dist < 2) continue;
                if (Vector3.Angle(f, Vector3.ProjectOnPlane(to, Vector3.up)) > RadarCone) continue; best = dist; target = d;
            }
            if (!target) { c.radar = -1; c.radarOver = false; return; }
            c.radar = PoliceKit.Mph(target.Car.Body.linearVelocity.magnitude); c.radarLimit = PoliceKit.LimitAt(target.Car.Body.position, out _); c.radarOver = c.radar > c.radarLimit + 1;
            var s = SpeederOf(target);
            if (s != null && !s.clocked && c.radarOver)
            {
                s.clocked = true; s.clockedAt = Time.time; s.clockedOver = Mathf.Max(1, Mathf.RoundToInt(c.radar - c.radarLimit)); Say(c, "S02", $"  {Mathf.RoundToInt(c.radar)} in a {c.radarLimit}  (+{s.clockedOver})"); c.log.Add($"clocked {s.clockedOver} over");
                var st = PoliceRadio.PlaceId(RoadNet.Shared, s.Car.Body.position); if (st != null && c == Cops[0]) PoliceRadio.Speak(new[] { "P10", st });
            }
            // Chastain Rush, Round 39: while the radar stays on a clocked speeder before the pull-over, its fastest reading counts (the first read
            // could catch it still speeding up, 1 mph over)
            else if (s != null && s.clocked && !s.caught && c.radarOver) s.clockedOver = Mathf.Max(s.clockedOver, Mathf.RoundToInt(c.radar - c.radarLimit));
        }
        void LightsOnTraffic(Cop c)
        {
            var near = new HashSet<TrafficDriver>();
            bool chasing = Speeders.Any(s => s.pursuit && !s.caught && !s.gone && s.Car && Vector3.Distance(s.Car.Body.position, c.car.Body.position) < PursuitRange + 10);
            if (c.siren && !chasing)
                foreach (var d in Traffic)
                {
                    if (Vector3.Distance(d.Car.Body.position, c.car.Body.position) > MeterRange) continue; var s = SpeederOf(d); if (s != null && s.clocked) continue;
                    near.Add(d); c.lightsOn[d] = (c.lightsOn.TryGetValue(d, out var t) ? t : 0) + Time.deltaTime;
                    if (c.lightsOn[d] >= NoViolationSeconds && c.penalised.Add(d)) { c.points -= NoViolationPenalty; c.penalties++; c.noViolations++; Say(c, "S07", $"  −{NoViolationPenalty}"); c.log.Add("no violation"); }
                }
            foreach (var d in c.lightsOn.Keys.ToList()) if (!near.Contains(d)) c.lightsOn.Remove(d);
        }
        void Run(Speeder s)
        {
            var car = s.Car; if (!car) { Speeders.Remove(s); return; }
            if (s.caught || s.gone) return;
            if (!s.clocked && Time.time - s.since > GetAwaySeconds) { Release(s); return; }
            if (s.clocked && !s.pursuit && Cops.Any(c => c.siren && Vector3.Distance(c.car.Body.position, car.Body.position) < PursuitRange)) { s.pursuit = true; if (Cops.Count > 0) Say(Cops[0], "S03"); }   // (Racer said S03 every frame: its bug, fixed)
            if (s.pursuit)
            {
                float fill = Mathf.Lerp(4, 8, Mathf.InverseLerp(10, 40, s.clockedOver));
                foreach (var c in Cops)
                {
                    bool close = c.siren && Vector3.Distance(c.car.Body.position, car.Body.position) < MeterRange;
                    s.meter[c.player] = Mathf.Clamp01(s.meter[c.player] + (close ? 1 : -1) * Time.deltaTime / fill);
                    if (s.meter[c.player] >= 1) { Catch(s, c, false); return; }
                }
            }
            if (s.rammedAt > 0 && Time.time - s.rammedAt < 3 && car.Body.linearVelocity.magnitude < 2) { Catch(s, CopOf(s.rammedBy), true); return; }
            if (s.clocked && Time.time - s.clockedAt >= GetAwaySeconds) { s.gone = true; GotAway++; foreach (var c in Cops) Say(c, "S05"); foreach (var c in Cops) c.log.Add("got away"); Release(s); }
        }
        void Catch(Speeder s, Cop c, bool rammed)
        {
            if (c == null) return; s.caught = true; s.pursuit = false; s.stoppedUntil = Time.time + 6;
            int points = (CatchPoints + PerMph * s.clockedOver) / (rammed ? 2 : 1); c.points += points; c.catches++; c.fastest = Mathf.Max(c.fastest, s.clockedOver);
            Say(c, "S04", $"  +{points}{(rammed ? " (rammed: half points)" : "")}  ({s.clockedOver} mph over)"); c.log.Add($"caught {s.clockedOver} over{(rammed ? " rammed" : "")}");
            if (s.driver) s.driver.SpeedOverride = 0;
        }
        public void Hit(Cop c, ArcadeVehicle other, float speed)
        {
            if (State != Phase.Running && State != Phase.Overtime) return;
            if (Cops.Any(x => x.car == other) || speed < 2.5f) return;
            if (c.lastHit.TryGetValue(other, out var t) && Time.time - t < 3) return; c.lastHit[other] = Time.time;
            var s = SpeederOf(other);
            if (s != null && s.clocked && !s.caught) { s.rammedAt = Time.time; s.rammedBy = c.player; return; }
            c.points -= HitPenalty; c.penalties++; c.hits++; Say(c, "S06", $"  −{HitPenalty}"); c.log.Add("hit traffic");
        }
        void Say(Cop c, string id, string note = "")
        {
            c.line = PoliceRadio.TextOf(id) + note; c.lineUntil = Time.unscaledTime + 3.5f;
            if (c == Cops[0]) PoliceRadio.Speak(new[] { id }, id == "S04" || id == "S05" || id == "S08");
        }
        public IEnumerable<Vector3> ClockedMarks => Speeders.Where(s => s.clocked && !s.caught && !s.gone && s.Car).Select(s => s.Car.transform.position);
        public float MeterFor(int player) => Speeders.Where(s => s.pursuit && !s.caught && !s.gone).Select(s => s.meter[player]).DefaultIfEmpty(0).Max();

        [System.Serializable] public sealed class Entry { public string name; public int points, catches, minutes; public string date; }
        [System.Serializable] public sealed class Board { public List<Entry> entries = new(); }
        public const string BoardFile = "speed-patrol-v1.json";
        public readonly List<int> Places = new();
        void Finish()
        {
            State = Phase.Done; foreach (var c in Cops) { c.siren = false; c.lights.Siren = false; PoliceKit.Hold(held, c.car, false); }
            foreach (var s in Speeders.ToList()) Release(s); foreach (var t in Traffic) t.SpeedOverride = -1;
            Places.Clear(); var board = PoliceKit.Load<Board>(BoardFile);
            foreach (var c in Cops)
            {
                if (HighMode.ForPlayer(c.player)) { Places.Add(0); continue; }   // Round 84 (Racer 1.2): HIGH MODE patrols are not put on the (normal) Top 10
                var e = new Entry { name = PoliceKit.NameOf(c.player), points = c.points, catches = c.catches, minutes = PoliceSetup.Minutes, date = System.DateTime.Now.ToString("yyyy-MM-dd") };
                board.entries.Add(e); var top = board.entries.Where(x => x.minutes == e.minutes).OrderByDescending(x => x.points).ThenBy(x => board.entries.IndexOf(x)).Take(10).ToList();
                Places.Add(top.IndexOf(e) + 1);
                board.entries = board.entries.Where(x => x.minutes != e.minutes || top.Contains(x)).ToList();
            }
            PoliceKit.Save(BoardFile, board); Finished?.Invoke(this);
        }
        public static List<Entry> Top(int minutes) => PoliceKit.Load<Board>(BoardFile).entries.Where(e => e.minutes == minutes).OrderByDescending(e => e.points).Take(10).ToList();
        public int Winner => Cops.Count < 2 ? 0 : Cops[0].points > Cops[1].points ? 1 : Cops[1].points > Cops[0].points ? 2 : 0;
        public void Rematch() => BeginRound();
    }

    public sealed class PatrolContacts : MonoBehaviour
    {
        public SpeedPatrol.Cop Cop; public SpeedPatrol Patrol;
        void OnCollisionEnter(Collision c)
        {
            if (!Patrol || Cop == null) return; var other = c.collider.GetComponentInParent<ArcadeVehicle>(); if (!other || other.gameObject == gameObject) return;
            Patrol.Hit(Cop, other, c.relativeVelocity.magnitude);
        }
    }
}
