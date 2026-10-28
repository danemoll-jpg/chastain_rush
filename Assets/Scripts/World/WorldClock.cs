using UnityEngine;

namespace ChastainRush
{
    // Round 79 (A, Dan: "not sure I understand why the train wont be in the replays. It is a feature of that track."): the clock the world's timetables run
    // on (the trains' (RailTrains), the traffic signals' phases (TrafficSignals), the airport's circuit plane (AirportCircuit)). Time.time, except while a
    // replay plays: then the time it was at that moment of the race (ReplayRecorder keeps it, ReplayViewer sets Replay), so they are as they were.
    public static class WorldClock
    {
        public static float Replay = float.NaN;
        public static bool Replaying => !float.IsNaN(Replay);
        public static float Now => Replaying ? Replay : Time.time;
    }
}
