using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 15 (item 6): one car in a race, the player or an AI driver, and where it stands under the race's rules
    // (RaceController: gates in order, 5 s per missed checkpoint, shortcuts forgive what they skip). Before Round 15 these
    // were RaceController's own fields for the one car; every racer now has its own, so the AI drivers race under exactly the
    // same rules as the player. Times are on the race clock (seconds since GO); a racer's own clock starts when it first
    // crosses the start line (Racer's rolling start), so its laps run gate to gate and its total is the sum of its laps.
    public sealed class RaceEntrant
    {
        public string Name;
        public ArcadeVehicle Car;
        public RoadDriver Driver;          // null for a human
        // Round 37 (split screen): 1 = the player, 2 = split screen's player 2 (a human, or the AI driving their vehicle), 0 = an AI rival
        public int Seat;
        public bool IsPlayer => Seat == 1;
        public bool IsHuman => Seat == 1 || Seat == 2 && Driver == null;
        public string Message = ""; public float MessageUntil;   // Round 37: this racer's own line on their half of the screen
        public int RaceRank;                                     // Round 37: player 2's race on the Top 10
        public BoxCollider Box;
        public Vector3 PrevPos;
        public bool ClockStarted;
        public bool LapAway; public float NextAwayCheck;   // Round 34 (A6): this lap has gone round to its far half (or passed a checkpoint)
        public float StartAt, LapStart;    // race clock at its first start-line crossing, and at the start of its lap
        public int CompletedLaps, NextCheckpoint, MissedTotal;
        public float PenaltyTotal, LapPenalty;
        public RaceShortcut ActiveShortcut;
        public readonly List<float> LapTimes = new List<float>();
        public readonly List<int> Missed = new List<int>(), Forgiven = new List<int>();
        public readonly List<string> PenaltyLog = new List<string>(), LapLog = new List<string>();
        public readonly List<string> ShortcutsTaken = new List<string>();
        public bool Finished, Estimated;
        public float FinalTotal;           // its own time: from its start-line crossing, with every penalty
        public float FinishedAt;           // race clock when it crossed the line, with every penalty (the finishing order)
        public readonly AiFinishEstimate Estimate = new AiFinishEstimate();
        public string VehicleName => Car && Car.GetComponent<VehicleConfiguration>() ? Car.GetComponent<VehicleConfiguration>().Profile.Name : "";

        public void Reset()
        {
            ClockStarted = LapAway = false; NextAwayCheck = 0f; StartAt = LapStart = 0f; CompletedLaps = NextCheckpoint = MissedTotal = 0;
            PenaltyTotal = LapPenalty = 0f; ActiveShortcut = null; Finished = Estimated = false; FinalTotal = FinishedAt = 0f;
            LapTimes.Clear(); Missed.Clear(); Forgiven.Clear(); PenaltyLog.Clear(); LapLog.Clear(); ShortcutsTaken.Clear();
            if (Car) PrevPos = Car.transform.position;
        }

        // a reset or a placement is a teleport: it must not count as crossing anything
        public void NotifyTeleported() { if (Car) PrevPos = Car.transform.position; }
    }
}
