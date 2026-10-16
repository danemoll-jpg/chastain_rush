using System.Collections.Generic;
using System.Linq;
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
    // Chastain Rush, Round 36 (Part A1): one player; started from the Free Roam menu (GameMenus: STUNT EVENT), run inside Free Roam (Racer runs it
    // on its split-screen machinery), the results on a menu page. Game layer: the park is world, this event, its start and its scoring are on top.
    // Round 37 (Part B): Racer's split-screen event (its Riders): each player their own points, combos and medal, both held 3 s on the start line,
    // the results when every player is done ("WAITING FOR P2" on the half of one who is), each human's result on the Stunt event Top 10 under
    // their name, the winner celebrates (Racer's winner shot is not in this game). One player is rider 1, as before.
    public sealed class StuntEvent : MonoBehaviour
    {
        public const float StartPause = 3;
        public static StuntEvent Current { get; private set; }
        public enum Phase { Starting, Running, Done }
        public sealed class Rider
        {
            public int player; public ArcadeVehicle car; public ArcadeActivities acts; public string vehicle;
            public int points, best, combos, outside, place, medal; public string line = ""; public float lineUntil; public bool closed;
            public StuntProgress.Entry entry;
            public int goalMedal; public float goalFlashUntil, still;
            public readonly List<string> log = new(); public System.Action<int, bool> banked;
            public readonly List<StuntProgress.Combo> entered = new();
        }
        public readonly List<Rider> Riders = new();
        public Phase State { get; private set; }
        public int Round { get; private set; }
        public float Clock { get; private set; }
        public float Limit => StuntProgress.Minutes * 60;
        public float Left => Mathf.Max(0, RoundLimit - Clock);
        public const float StillLimit = 5, MedalMoment = 2.5f;
        public bool Overtime => State == Phase.Running && Clock >= RoundLimit;
        public bool Finishing(int player) { var r = Of(player); return Overtime && r != null && !r.closed; }
        public Rider Of(int player) => Riders.FirstOrDefault(r => r.player == player);
        // player 1's (the one-player event's) results, as before Round 37
        Rider R1 => Of(1);
        public int Points => R1?.points ?? 0;
        public int Best => R1?.best ?? 0;
        public int Combos => R1?.combos ?? 0;
        public int Outside => R1?.outside ?? 0;
        public int Place => R1?.place ?? 0;
        public int Medal => R1?.medal ?? 0;
        public int GoalMedal => R1?.goalMedal ?? 0;
        public float GoalFlashUntil => R1?.goalFlashUntil ?? 0;
        public string Line => R1?.line ?? ""; public float LineUntil => R1?.lineUntil ?? 0;
        public string Vehicle => R1?.vehicle;
        public bool Closed => R1 != null && R1.closed;
        public List<string> Log => R1 != null ? R1.log : new List<string>();
        public List<StuntProgress.Combo> Entered => R1 != null ? R1.entered : new List<StuntProgress.Combo>();
        public StuntProgress.Entry Entry => R1?.entry;
        public ArcadeActivities Acts => R1?.acts;
        public bool TwoPlayers => Riders.Count > 1;
        // the winner of a two-player event (0: one player, or a draw)
        public int Winner => Riders.Count < 2 ? 0 : Riders[0].points > Riders[1].points ? 1 : Riders[1].points > Riders[0].points ? 2 : 0;
        // 0.108 Part D: a combo banked by this vehicle counts for the Top 10 (always outside the event; in it, while that player's round still counts)
        public static bool Counts(ArcadeVehicle car) { var e = Current; if (!e) return true; var r = e.Riders.FirstOrDefault(x => x.car == car); return r == null || e.State == Phase.Running && !r.closed; }
        // 0.107 Part C: a vehicle taking part in the running event (the resets put it back in the park)
        public static bool InEvent(ArcadeVehicle car) => Current && car && Current.State != Phase.Done && Current.Riders.Any(r => r.car == car);
        public bool Holds(ArcadeVehicle car) => held.Contains(car);
        public static System.Action<StuntEvent> Finished;   // the menus show the results
        readonly HashSet<ArcadeVehicle> held = new(); float phaseAt, overtime;
        // checks only: a shorter round (seconds); never set in play
        public static float TestLimit = -1;
        float RoundLimit => TestLimit > 0 ? TestLimit : Limit;
        ArcadeVehicle car1, car2; ArcadeActivities acts2;

        // player 2 (split screen) rides too when their vehicle and its scoring are given
        public static StuntEvent Begin(ArcadeVehicle player, ArcadeVehicle player2 = null, ArcadeActivities activities2 = null)
        {
            if (!StuntPark.Current || !player) return null;
            if (!Current) Current = new GameObject("Stunt event").AddComponent<StuntEvent>();
            Current.car1 = player; Current.car2 = player2; Current.acts2 = activities2;
            Current.BeginRound(); return Current;
        }
        public static void End() { if (Current) { Current.Release(); Destroy(Current.gameObject); Current = null; } }
        void OnDestroy() { Unhook(); Release(); if (Current == this) Current = null; }
        void Release() { foreach (var c in held.ToList()) Hold(c, false); }
        void BeginRound()
        {
            Unhook(); Release(); Riders.Clear(); Round++; Clock = 0; State = Phase.Starting; phaseAt = Time.time; overtime = 0;
            StuntPark.Current.RestoreBreakables();
            // to the start line, facing the first line of ramps (player 2 beside)
            var respawn = car1.GetComponent<VehicleRespawn>();
            if (respawn) respawn.PlaceAt(StuntPark.StartPos, StuntPark.Current.startYaw); else { car1.Body.position = StuntPark.StartPos; car1.Body.rotation = StuntPark.StartRot; }
            foreach (var c in FindObjectsByType<ChaseCamera>(FindObjectsSortMode.None)) if (c.target == car1.transform) c.Snap();
            Add(1, car1, car1.GetComponent<ArcadeActivities>());
            if (car2 && acts2)
            {
                SplitSession.PlaceBeside(car2, car1.transform, 4.5f);
                foreach (var c in FindObjectsByType<ChaseCamera>(FindObjectsSortMode.None)) if (c.target == car2.transform) c.Snap();
                Add(2, car2, acts2);
            }
            foreach (var r in Riders) { r.acts.NewSession(); Hold(r.car, true); r.log.Add($"start: {VehicleProfile.Find(r.vehicle).Name} at {r.car.Body.position}"); }
        }
        void Add(int player, ArcadeVehicle car, ArcadeActivities acts)
        {
            if (!car || !acts) return;
            var r = new Rider { player = player, car = car, acts = acts, vehicle = car.GetComponent<VehicleConfiguration>().profileId };
            r.banked = (points, inPark) =>
            {
                if (State != Phase.Running || r.closed) return;
                if (!inPark) { r.outside++; Say(r, "Outside the park: not counted"); r.log.Add($"{Clock:0.0}s outside the park {points}"); return; }
                if (r.acts.LastComboPlace > 0 && StuntProgress.LastComboEntry != null) r.entered.Add(StuntProgress.LastComboEntry);
                r.points += points; r.combos++; r.best = Mathf.Max(r.best, points); Say(r, $"+{points:N0}");
                r.log.Add($"{Clock:0.0}s +{points} ({r.acts.LastStunt}; x{r.acts.LastComboLinks}: {r.acts.LastComboTricks}){(r.acts.LastComboPlace > 0 ? $" Top 10 combo #{r.acts.LastComboPlace}" : "")}");
                int m = StuntProgress.MedalFor(r.points); if (m > r.goalMedal) { r.goalMedal = m; r.goalFlashUntil = Time.unscaledTime + MedalMoment; } // 0.108 Part B
            };
            acts.ComboBanked += r.banked; Riders.Add(r);
        }
        void Unhook() { foreach (var r in Riders) if (r.acts) r.acts.ComboBanked -= r.banked; }
        void Say(Rider r, string text) { r.line = text; r.lineUntil = Time.unscaledTime + 3; }
        void Hold(ArcadeVehicle car, bool on)
        {
            if (!car) return;
            if (on) { held.Add(car); car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; car.Body.isKinematic = true; car.enabled = false; }
            else { held.Remove(car); car.Body.isKinematic = false; car.enabled = true; }
            var input = car.GetComponent<VehicleInput>(); if (input) input.enabled = !on;
        }
        void Update()
        {
            if (Riders.Count == 0 || State == Phase.Done || GameMenus.Open) return;
            if (State == Phase.Starting) { if (Time.time - phaseAt >= StartPause) { foreach (var r in Riders) Hold(r.car, false); State = Phase.Running; } return; }
            if (Clock < RoundLimit)
            {
                Clock = Mathf.Min(RoundLimit, Clock + Time.deltaTime); if (Clock < RoundLimit) return; overtime = 0;
                foreach (var r in Riders) { r.still = 0; r.log.Add(r.acts.ComboLinks > 0 ? $"0:00 with a combo going (x{r.acts.ComboLinks}, {r.acts.ComboTotal:N0} pts{(r.acts.StuntBusy ? ", in the air" : $", {r.acts.ComboLeft:0.0} s to chain")}): overtime" : r.acts.StuntBusy ? "0:00 in the air: overtime" : "0:00: done"); }
            }
            else overtime += Time.deltaTime;
            // 0.108 Part A: a player is done when nothing is going any more (no combo, not in the air); stopped for StillLimit s ends it too
            foreach (var r in Riders)
            {
                if (r.closed) continue;
                r.still = r.car && r.car.Body.linearVelocity.magnitude < 1 ? r.still + Time.deltaTime : 0;
                if (r.acts.ComboLinks == 0 && !r.acts.StuntBusy) { r.closed = true; if (overtime > 0) r.log.Add($"done {overtime:0.0}s after 0:00"); }
                else if (r.still >= StillLimit) { r.acts.EndStuntCombo(); r.closed = true; r.log.Add($"stopped for {StillLimit:0} s, {overtime:0.0}s after 0:00: ended"); }
            }
            if (Riders.All(r => r.closed)) Finish();
        }
        void Finish()
        {
            foreach (var r in Riders) if (!r.closed) { r.acts.EndStuntCombo(); r.closed = true; }
            State = Phase.Done; Unhook();
            foreach (var r in Riders)
            {
                r.medal = StuntProgress.MedalFor(r.points);
                bool human = r.player == 1 || !SplitScreen.P2Ai;   // Racer: the AI's results are never recorded
                r.entry = human ? StuntProgress.Finish(SplitScreen.Active ? SplitScreen.NameOf(r.player) : PlayerNames.Player, r.vehicle, r.points, r.best) : null;
            }
            // 0.110 Part A: each place read after every player's entry is in (two players next to each other: #5 and #6, not both #5)
            foreach (var r in Riders) { r.place = StuntProgress.PlaceOf(r.entry); r.log.Add($"results: {r.points:N0} pts, {MedalUi.Name(r.medal)}, best combo {r.best:N0}, tricks {r.acts.Tricks}, bails {r.acts.Bails}, Top 10 #{r.place}"); }
            int w = Winner; if (w > 0) Of(w)?.car.GetComponent<RiderGestures>()?.Celebrate();
            Finished?.Invoke(this);
        }
        public void Rematch() { if (car1) BeginRound(); }
        // the event's panel lines (ActivityHud draws them for one player; SplitHud draws each half's): the clock, the points and the next medal
        public string Panel
        {
            get
            {
                if (State == Phase.Starting) { int n = Mathf.CeilToInt(StartPause - (Time.time - phaseAt)); return $"STUNT EVENT\n{(n > 0 ? n.ToString() : "GO!")}"; }
                if (State == Phase.Done || R1 == null) return "";
                var acts = R1.acts; float t = Left; string clock = Overtime ? (acts.StuntBusy ? "FINISH THE TRICK!" : $"COMBO OVERTIME  {acts.ComboLeft:0.0} s") : $"{(int)t / 60}:{(int)t % 60:00}";
                int next = GoalMedal < 3 ? StuntProgress.Targets[GoalMedal] : 0;
                string goal = Time.unscaledTime < GoalFlashUntil ? $"{MedalUi.Name(GoalMedal)} REACHED!" : next > 0 ? $"next: {MedalUi.Name(GoalMedal + 1)} {next:N0}" : "GOLD";
                return $"STUNT EVENT   {clock}\n{Points:N0} pts   ·   {goal}" + (Time.unscaledTime < LineUntil ? "\n" + Line : "");
            }
        }
    }
}
