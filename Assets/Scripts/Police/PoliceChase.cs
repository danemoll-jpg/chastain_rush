using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): Racer's Police Chase (0.94 Part C, its 0.95 / 0.98 rules), Cop vs Runner, in Free Roam on this map.
    //   Two players (split screen): one is the cop in the patrol car, the other runs in their own vehicle; then the roles swap (two rounds). The runner
    //   starts on the road, the cop 80 m behind, released 3 s after. The bust meter fills while the cop is within 10 m and the runner is under 7 m/s
    //   (3 s to fill), drains otherwise; full = caught; at the round limit the runner got away. A runner's reset holds them 2 s. The longer run as the
    //   runner wins; getting away beats being caught; both away = a draw. Nothing recorded.
    //   One player: the cop, chasing an AI runner on the full screen (one round; being chased by AI cops is Getaway: Racer 0.98's rule, solo play never
    //   swaps roles). The AI runner drives the roads flat out (TrafficDriver, its speed held up near its top) and turns away from the cop.
    public sealed class PoliceChase : MonoBehaviour
    {
        public const float CatchDistance = 10, CatchSpeed = 7, FillSeconds = 3, CopDelay = 3, CopBehind = 80, ResetHold = 2, EndPause = 4;
        public static PoliceChase Current { get; private set; }
        public static System.Action<PoliceChase> Finished;
        public enum Phase { Starting, Running, Caught, Away, Done }
        public sealed class Run { public int runner; public bool caught; public float seconds; public string runnerVehicle; }
        public readonly List<Run> Runs = new();
        public int Round { get; private set; }
        public Phase State { get; private set; }
        public float Meter { get; private set; }
        public float Clock { get; private set; }
        public float Limit => TestLimit > 0 ? TestLimit : PoliceSetup.Minutes * 60; public static float TestLimit = -1;   // checks only
        public bool Solo => !SplitScreen.Active || SplitScreen.P2Ai;
        public int Rounds => Solo ? 1 : 2;
        public int Cop => Solo ? 1 : Round == 0 ? PoliceSetup.CopFirst : 3 - PoliceSetup.CopFirst;
        public int Runner => Solo ? 2 : 3 - Cop;
        public bool Siren { get; private set; }
        float phaseAt, holdUntil; PoliceLights lights; int resets; ArcadeVehicle aiRunner; string copWas; int copWasColour = -1; ArcadeVehicle copCar;
        readonly HashSet<ArcadeVehicle> held = new();
        public ArcadeVehicle CarOf(int player) => Solo ? (player == 1 ? PoliceKit.Player(1) : aiRunner) : PoliceKit.Player(player);
        public bool Holds(ArcadeVehicle car) => held.Contains(car);
        public string Role(int player) => player == Cop ? "COP" : "RUNNER";

        public static PoliceChase Begin() { End(); var g = new GameObject("Police Chase").AddComponent<PoliceChase>(); Current = g; g.Runs.Clear(); g.Round = 0; g.BeginRound(); return g; }
        public static void End() { if (Current) { Destroy(Current.gameObject); Current = null; } }
        void OnDestroy()
        {
            foreach (var c in held.ToList()) PoliceKit.Hold(held, c, false, c == aiRunner); PoliceKit.Unwear(copCar, copWas, copWasColour);
            if (aiRunner) Destroy(aiRunner.gameObject); if (Current == this) Current = null;
        }
        public void BeginRound()
        {
            PoliceKit.Unwear(copCar, copWas, copWasColour); copCar = null;
            var net = RoadNet.Shared; var p1 = PoliceKit.Player(1);
            int node = net.Nearest(p1.Body.position, out _, 400); var f = node >= 0 ? net.Tangent(node) : p1.transform.forward; f.y = 0; if (Vector3.Dot(f, p1.transform.forward) < 0) f = -f;
            var p = node >= 0 ? net.P[node] : p1.Body.position; var right = Vector3.Cross(Vector3.up, f).normalized;
            if (Solo && !aiRunner) aiRunner = MakeAiRunner(p1);
            var runner = CarOf(Runner); var cop = CarOf(Cop); if (!runner || !cop) return;
            copCar = cop; PoliceKit.Wear(cop, PoliceSetup.CopVehicle, new Color(.03f, .03f, .035f), out copWas, out copWasColour);
            int back = node >= 0 ? net.Ahead(node, -f, CopBehind) : -1; var pc = back >= 0 ? net.P[back] : p - f * CopBehind;
            PoliceKit.Put(runner, p + right * 2.2f, f); PoliceKit.Put(cop, pc + right * 2.2f, f);
            if (runner == aiRunner) CruiseAway(runner);
            Meter = 0; Clock = 0; State = Phase.Starting; phaseAt = Time.time; holdUntil = 0; Siren = false;
            PoliceKit.Hold(held, runner, true, runner == aiRunner); PoliceKit.Hold(held, cop, true);
            lights = cop.GetComponent<PoliceLights>(); if (!lights) lights = cop.gameObject.AddComponent<PoliceLights>(); lights.Target = runner.transform; lights.Siren = false; lights.Rebuild();
            var rs = runner.GetComponent<VehicleRespawn>(); if (rs) { rs.Respawned -= RunnerReset; rs.Respawned += RunnerReset; } resets = 0;
        }
        // the AI runner: the player's vehicle as an AI clone (RaceController's recipe), the runner's paint, on a road driver held near its top speed
        ArcadeVehicle MakeAiRunner(ArcadeVehicle template)
        {
            var clone = Instantiate(template.gameObject); clone.name = "AI runner";
            foreach (var mb in clone.GetComponents<MonoBehaviour>())
            {
                if (mb is VehicleInput || mb is VehicleRespawn) { mb.enabled = false; continue; }
                if (mb is ArcadeVehicle || mb is VehicleConfiguration || mb is RampHold || mb is WaterFeedback || mb is VehicleLights || mb is RiderGestures) continue;
                mb.enabled = false; if (mb is VehicleAudio) DestroyImmediate(mb); else Destroy(mb);
            }
            foreach (var src in clone.GetComponents<AudioSource>()) Destroy(src);
            var car = clone.GetComponent<ArcadeVehicle>(); var cfg = clone.GetComponent<VehicleConfiguration>(); cfg.Apply(string.IsNullOrEmpty(SplitScreen.P2Vehicle) ? "fastback" : SplitScreen.P2Vehicle); cfg.SetPaint(new Color(.95f, .55f, .1f));
            car.enabled = false; return car;
        }
        void CruiseAway(ArcadeVehicle car)
        {
            var d = PoliceKit.Cruise(car); if (d) d.SpeedOverride = car.topSpeed * .82f;
        }
        void RunnerReset() { if (State != Phase.Running) return; resets++; holdUntil = Time.time + ResetHold; PoliceKit.Hold(held, CarOf(Runner), true, CarOf(Runner) == aiRunner); }
        public int RunnerResets => resets;
        void Update()
        {
            if (Time.timeScale == 0) return;
            if (!PoliceKit.FreeRoam) { End(); return; }
            var runner = CarOf(Runner); var cop = CarOf(Cop); if (!runner || !cop) return;
            foreach (var car in held) if (car && (!car.Body.isKinematic || car.enabled)) { car.Body.isKinematic = true; car.enabled = false; }
            float t = Time.time - phaseAt;
            switch (State)
            {
                case Phase.Starting:
                    if (t >= 3 && held.Contains(runner) && holdUntil == 0) { PoliceKit.Hold(held, runner, false, runner == aiRunner); holdUntil = -1; }
                    if (t >= 3 + CopDelay) { PoliceKit.Hold(held, cop, false); State = Phase.Running; Siren = true; holdUntil = 0; }
                    if (t >= 3) Clock = t - 3;
                    break;
                case Phase.Running:
                    Clock += Time.deltaTime;
                    if (holdUntil > 0 && Time.time >= holdUntil) { holdUntil = 0; PoliceKit.Hold(held, runner, false, runner == aiRunner); }
                    bool close = Vector3.Distance(cop.Body.position, runner.Body.position) < CatchDistance && runner.Body.linearVelocity.magnitude < CatchSpeed;
                    Meter = Mathf.Clamp01(Meter + (close ? 1 : -1) * Time.deltaTime / FillSeconds);
                    if (Toggle()) Siren = !Siren;
                    if (Meter >= 1) EndRound(true); else if (Clock >= Limit) { Clock = Limit; EndRound(false); }
                    break;
                case Phase.Caught: case Phase.Away:
                    if (t >= EndPause) { if (Round + 1 < Rounds) { Round++; BeginRound(); } else Finish(); }
                    break;
            }
            if (lights) lights.Siren = Siren && (State == Phase.Running || State == Phase.Caught);
            // the AI runner: the cop round in front of it (within 120 m ahead): it turns away down the next road (a new cruise from where it is)
            if (runner == aiRunner && State == Phase.Running && Time.time > nextTurn)
            {
                var ahead = Vector3.Dot(cop.Body.position - runner.Body.position, runner.transform.forward);
                if (ahead > 0 && ahead < 120 && Vector3.Distance(cop.Body.position, runner.Body.position) > 25) { nextTurn = Time.time + 12; runner.transform.rotation = Quaternion.LookRotation(-runner.transform.forward); CruiseAway(runner); }
            }
        }
        float nextTurn;
        public static bool ScriptedToggle;   // checks
        bool Toggle()
        {
            if (ScriptedToggle) { ScriptedToggle = false; return true; }
            var device = SplitScreen.Active ? (Cop == 1 ? SplitScreen.P1Device : SplitScreen.P2Device) : null;
            if (device is Gamepad g) return g.rightShoulder.wasPressedThisFrame;
            if (device is Keyboard k) return k.hKey.wasPressedThisFrame;
            if (!SplitScreen.Active) return (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame);
            return false;
        }
        void EndRound(bool caught)
        {
            Runs.Add(new Run { runner = Runner, caught = caught, seconds = Clock, runnerVehicle = CarOf(Runner).GetComponent<VehicleConfiguration>().profileId });
            State = caught ? Phase.Caught : Phase.Away; phaseAt = Time.time; PoliceKit.Hold(held, CarOf(Runner), caught, CarOf(Runner) == aiRunner); PoliceKit.Hold(held, CarOf(Cop), caught);
            PoliceRadio.Speak(new[] { caught ? "R03" : "R04" }, true);
        }
        void Finish() { State = Phase.Done; foreach (var c in held.ToList()) PoliceKit.Hold(held, c, false, c == aiRunner); PoliceKit.Unwear(copCar, copWas, copWasColour); copCar = null; if (aiRunner) { Destroy(aiRunner.gameObject); aiRunner = null; } Finished?.Invoke(this); }
        public void Rematch() { Runs.Clear(); Round = 0; BeginRound(); }
        public int Winner
        {
            get
            {
                if (Runs.Count < 2) return 0;
                var a = Runs[0]; var b = Runs[1];
                if (!a.caught && !b.caught) return 0;
                if (a.caught != b.caught) return a.caught ? b.runner : a.runner;
                return a.seconds > b.seconds ? a.runner : b.seconds > a.seconds ? b.runner : 0;
            }
        }
        public string SoloVerdict { get { if (Runs.Count == 0) return ""; var r = Runs[0]; return r.caught ? "CAUGHT in " + PoliceKit.Mss(r.seconds) : "GOT AWAY"; } }
        public string Line(Run r) => $"{(Solo && r.runner == 2 ? "The AI" : PoliceKit.NameOf(r.runner))} ran ({VehicleProfile.Find(r.runnerVehicle).Name}): {(r.caught ? "CAUGHT after " + PoliceKit.Mss(r.seconds) : "GOT AWAY (" + PoliceKit.Mss(r.seconds) + ")")}";
    }
}
