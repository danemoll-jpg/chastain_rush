using System.Collections.Generic;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // 0.106 Part D: the Stunt event at the stunt park: StuntProgress.Minutes to score as many points as possible. The player starts on the start line
    // (held 3 s), then every combo banked inside the park counts (ArcadeActivities: jumps, flips, rolls, spins, the loop and the barrel roll, chained
    // for a multiplier; a bail ends a combo). The clock runs only while driving (a pause keeps the clock and the points). Results: the points and
    // medal (StuntProgress.Targets), best combo, tricks and bails; the Stunt event Top 10.
    // 0.107 Part B: a combo whose flights took off in the park counts wherever they land. 0.108 Part A: at 0:00 a player with a combo going (in the
    // air, in the loop, or on the ground inside the chain time) keeps going ("COMBO OVERTIME"): every trick chained in time counts; done when the
    // combo ends (banked as always); stopped for StillLimit seconds ends it. 0.108 Part B: the next medal on the HUD.
    // Chastain Rush, Round 36 (Part A1): one player (Racer's split-screen, its AI second player and the winner's shot are left behind); started from
    // the Free Roam menu (GameMenus: STUNT EVENT), run inside Free Roam (Racer runs it on its split-screen machinery), the results on a menu page.
    // Game layer: the park is world, this event, its start and its scoring are on top of it.
    public sealed class StuntEvent : MonoBehaviour
    {
        public const float StartPause = 3;
        public static StuntEvent Current { get; private set; }
        public enum Phase { Starting, Running, Done }
        public Phase State { get; private set; }
        public int Round { get; private set; }
        public float Clock { get; private set; }
        public float Limit => StuntProgress.Minutes * 60;
        public float Left => Mathf.Max(0, RoundLimit - Clock);
        public const float StillLimit = 5, MedalMoment = 2.5f;
        public int Points { get; private set; }
        public int Best { get; private set; }
        public int Combos { get; private set; }
        public int Outside { get; private set; }
        public int Place { get; private set; }
        public int Medal { get; private set; }
        public int GoalMedal { get; private set; }
        public float GoalFlashUntil { get; private set; }
        public string Line { get; private set; } = ""; public float LineUntil { get; private set; }
        public string Vehicle { get; private set; }
        public bool Closed { get; private set; }
        public readonly List<string> Log = new();
        public readonly List<StuntProgress.Combo> Entered = new(); // 0.109 Part B: this event's entries in the Top 10 Best Combos
        public StuntProgress.Entry Entry { get; private set; }
        public ArcadeActivities Acts => acts;
        public bool Overtime => State == Phase.Running && Clock >= RoundLimit;
        // 0.108 Part D: a combo banked by this vehicle counts for the Top 10 (always outside the event; in it, while the round still counts)
        public static bool Counts(ArcadeVehicle car) { var e = Current; if (!e || e.car != car) return true; return e.State == Phase.Running && !e.Closed; }
        // 0.107 Part C: a vehicle taking part in the running event (the resets put it back in the park)
        public static bool InEvent(ArcadeVehicle car) => Current && car && Current.car == car && Current.State != Phase.Done;
        public static System.Action<StuntEvent> Finished;   // the menus show the results
        ArcadeVehicle car; ArcadeActivities acts; float phaseAt, still, overtime; bool held;
        // checks only: a shorter round (seconds); never set in play
        public static float TestLimit = -1;
        float RoundLimit => TestLimit > 0 ? TestLimit : Limit;

        public static StuntEvent Begin(ArcadeVehicle player)
        {
            if (!StuntPark.Current || !player) return null;
            if (!Current) Current = new GameObject("Stunt event").AddComponent<StuntEvent>();
            Current.BeginRound(player); return Current;
        }
        public static void End() { if (Current) { Current.Hold(false); Destroy(Current.gameObject); Current = null; } }
        void OnDestroy() { Unhook(); Hold(false); if (Current == this) Current = null; }
        void BeginRound(ArcadeVehicle player)
        {
            Unhook(); car = player; acts = car.GetComponent<ArcadeActivities>(); Vehicle = car.GetComponent<VehicleConfiguration>().profileId;
            Round++; Clock = 0; State = Phase.Starting; phaseAt = Time.time; Points = Best = Combos = Outside = Place = Medal = GoalMedal = 0; Closed = false; Entry = null; Log.Clear(); Entered.Clear(); still = overtime = 0;
            StuntPark.Current.RestoreBreakables();
            // to the start line, facing the first line of ramps
            var respawn = car.GetComponent<VehicleRespawn>();
            if (respawn) respawn.PlaceAt(StuntPark.StartPos, StuntPark.Current.startYaw); else { car.Body.position = StuntPark.StartPos; car.Body.rotation = StuntPark.StartRot; }
            FindAnyObjectByType<ChaseCamera>()?.Snap();
            if (acts) { acts.NewSession(); acts.ComboBanked += Banked; }
            Hold(true);
            Log.Add($"start: {VehicleProfile.Find(Vehicle).Name} at {StuntPark.StartPos}");
        }
        void Banked(int points, bool inPark)
        {
            if (State != Phase.Running || Closed) return;
            if (!inPark) { Outside++; Say("Outside the park: not counted"); Log.Add($"{Clock:0.0}s outside the park {points}"); return; }
            if (acts.LastComboPlace > 0 && StuntProgress.LastComboEntry != null) Entered.Add(StuntProgress.LastComboEntry);
            Points += points; Combos++; Best = Mathf.Max(Best, points); Say($"+{points:N0}"); Log.Add($"{Clock:0.0}s +{points} ({acts.LastStunt}; x{acts.LastComboLinks}: {acts.LastComboTricks}){(acts.LastComboPlace > 0 ? $" Top 10 combo #{acts.LastComboPlace}" : "")}");
            int m = StuntProgress.MedalFor(Points); if (m > GoalMedal) { GoalMedal = m; GoalFlashUntil = Time.unscaledTime + MedalMoment; } // 0.108 Part B
        }
        void Unhook() { if (acts) acts.ComboBanked -= Banked; }
        void Say(string text) { Line = text; LineUntil = Time.unscaledTime + 3; }
        void Hold(bool on)
        {
            if (!car) return;
            if (on == held) return; held = on;
            if (on) { car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; car.Body.isKinematic = true; car.enabled = false; }
            else { car.Body.isKinematic = false; car.enabled = true; }
            var input = car.GetComponent<VehicleInput>(); if (input) input.enabled = !on;
        }
        void Update()
        {
            if (!car || State == Phase.Done || GameMenus.Open) return;
            if (State == Phase.Starting) { if (Time.time - phaseAt >= StartPause) { Hold(false); State = Phase.Running; } return; }
            if (Clock < RoundLimit)
            {
                Clock = Mathf.Min(RoundLimit, Clock + Time.deltaTime); if (Clock < RoundLimit) return; overtime = 0; still = 0;
                Log.Add(acts.ComboLinks > 0 ? $"0:00 with a combo going (x{acts.ComboLinks}, {acts.ComboTotal:N0} pts{(acts.StuntBusy ? ", in the air" : $", {acts.ComboLeft:0.0} s to chain")}): overtime" : acts.StuntBusy ? "0:00 in the air: overtime" : "0:00: done");
            }
            else overtime += Time.deltaTime;
            // 0.108 Part A: done when nothing is going any more (no combo, not in the air); stopped for StillLimit s ends it too
            if (!Closed)
            {
                still = car.Body.linearVelocity.magnitude < 1 ? still + Time.deltaTime : 0;
                if (acts.ComboLinks == 0 && !acts.StuntBusy) { Closed = true; if (overtime > 0) Log.Add($"done {overtime:0.0}s after 0:00"); }
                else if (still >= StillLimit) { acts.EndStuntCombo(); Closed = true; Log.Add($"stopped for {StillLimit:0} s, {overtime:0.0}s after 0:00: ended"); }
            }
            if (Closed) Finish();
        }
        void Finish()
        {
            State = Phase.Done; Unhook();
            Medal = StuntProgress.MedalFor(Points);
            Entry = StuntProgress.Finish(PlayerNames.Player, Vehicle, Points, Best);
            Place = StuntProgress.PlaceOf(Entry);
            Log.Add($"results: {Points:N0} pts, {MedalUi.Name(Medal)}, best combo {Best:N0}, tricks {acts.Tricks}, bails {acts.Bails}, Top 10 #{Place}");
            Finished?.Invoke(this);
        }
        public void Rematch() { if (car) BeginRound(car); }
        // the event's panel lines (ActivityHud draws them): the clock, the points and the next medal
        public string Panel
        {
            get
            {
                if (State == Phase.Starting) { int n = Mathf.CeilToInt(StartPause - (Time.time - phaseAt)); return $"STUNT EVENT\n{(n > 0 ? n.ToString() : "GO!")}"; }
                if (State == Phase.Done) return "";
                float t = Left; string clock = Overtime ? (acts.StuntBusy ? "FINISH THE TRICK!" : $"COMBO OVERTIME  {acts.ComboLeft:0.0} s") : $"{(int)t / 60}:{(int)t % 60:00}";
                int next = GoalMedal < 3 ? StuntProgress.Targets[GoalMedal] : 0;
                string goal = Time.unscaledTime < GoalFlashUntil ? $"{MedalUi.Name(GoalMedal)} REACHED!" : next > 0 ? $"next: {MedalUi.Name(GoalMedal + 1)} {next:N0}" : "GOLD";
                return $"STUNT EVENT   {clock}\n{Points:N0} pts   ·   {goal}" + (Time.unscaledTime < LineUntil ? "\n" + Line : "");
            }
        }
    }
}
