using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 92 (F, Dan: "a race that you could race as a plane or a car ... one whole race should take the car and the plane about the same amount
    // of time"): WINGS & WHEELS. One race of two laps over the same streets: the car's (Data/routes/wings_and_wheels.json, the roads and the tube) and
    // the plane's (wings_and_wheels_air.json, the rings). Each route file names the other (RouteJson.twin). The race is raced on the file race setup
    // chose (RACE TYPE Street: the car's, Air: the plane's), and with mixed_ai the AI field mixes cars and planes, each on its own lap (RaceEntrant.Lap):
    // an opponent's vehicle (race setup's Opponent vehicles) picks its lap; "random" alternates car, plane, car. Every rule of the race reads the
    // racer's own lap (LapOf); the places compare the share of the race done (Score). Records: each lap its own boards (records_as), and every race
    // and lap also on a combined board (CombinedName) where cars and planes meet.
    public sealed partial class RaceController
    {
        public RaceRoute Twin { get; private set; }
        public RouteJson TwinData { get; private set; }
        public bool Mixed => Twin != null && RouteData != null && RouteData.mixed_ai;
        public RaceRoute LapOf(RaceEntrant e) => e != null && e.Lap != null ? e.Lap : Route;
        public RaceRoute CarLap => Twin != null && Route != null && Route.Air ? Twin : Route;
        public RaceRoute AirLap => Route != null && Route.Air ? Route : Twin != null && Twin.Air ? Twin : null;
        RouteJson CarData => Twin != null && Route != null && Route.Air ? TwinData : RouteData;
        RouteJson AirData => Route != null && Route.Air ? RouteData : Twin != null && Twin.Air ? TwinData : null;
        public const float AirStartSpeed = 75f;
        // the combined board's name: the pair's (the car file's name) and "(cars and planes)"
        public string CombinedName => Twin == null ? null : (Route.Air ? Twin.Name : Route.Name) + " (cars and planes)";
        public static string CombinedNameOf(RouteJson j) => j == null || string.IsNullOrEmpty(j.twin) || j.air ? null : j.name + " (cars and planes)";

        void LoadTwin(string dir)
        {
            Twin = null; TwinData = null;
            if (RouteData == null || string.IsNullOrEmpty(RouteData.twin)) return;
            var j = RouteJsonOf(RouteData.twin);
#if UNITY_EDITOR
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Data", "routes", RouteData.twin));
            if (System.IO.File.Exists(path)) j = System.IO.File.ReadAllText(path);
#endif
            if (string.IsNullOrEmpty(j)) { Debug.LogWarning($"[Race] {RouteData.name}: its twin {RouteData.twin} is not in the build"); return; }
            TwinData = JsonUtility.FromJson<RouteJson>(j);
            Twin = RaceRoute.Create(TwinData, TwinData.Allows(dir) ? dir : TwinData.direction);
            Twin.Laps = Route.Laps;   // one race: the same number of laps
            if (Twin.Air && High) foreach (var g in Twin.Checkpoints.Append(Twin.Gate)) { g.Ring *= HighMode.RingScale; g.RingW *= HighMode.RingScale; g.RingH *= HighMode.RingScale; }
        }

        RaceGate LapTarget(RaceEntrant e)
        {
            var r = LapOf(e);
            return !e.ClockStarted ? r.Gate : e.NextCheckpoint < r.Checkpoints.Count ? r.Checkpoints[e.NextCheckpoint] : r.Gate;
        }

        // run with this race's Route / RouteData set to one of its two laps (the spawners and the AI's road read them)
        void WithLap(RaceRoute r, RouteJson d, Action a)
        {
            var r0 = Route; var d0 = RouteData; Route = r; RouteData = d;
            try { a(); } finally { Route = r0; RouteData = d0; }
        }

        // an AI slot's vehicle in a mixed race: the saved choice if it is a car or a plane, else (random) car, plane, car
        string[] MixedVehicles(System.Random rng)
        {
            var picks = new string[AiCount];
            string[] chosen = AiVehiclesOverride ?? (CheckRun.Active ? null : SavedAiChoices());
            var cars = VehicleProfile.All.Where(v => CarData.AllowsVehicle(v.Id)).ToArray();
            var planes = VehicleProfile.All.Where(v => AirData.AllowsVehicle(v.Id)).ToArray();
            for (int i = 0; i < picks.Length; i++)
            {
                string id = chosen != null && i < chosen.Length ? chosen[i] : null;
                if (id == "car") id = cars[rng.Next(cars.Length)].Id;
                else if (id == "plane") id = planes[rng.Next(planes.Length)].Id;
                else if (string.IsNullOrEmpty(id) || id == "random" || VehicleProfile.IndexOf(id) < 0 || !(CarData.AllowsVehicle(id) || AirData.AllowsVehicle(id)))
                    id = i % 2 == 1 ? planes[rng.Next(planes.Length)].Id : cars[rng.Next(cars.Length)].Id;
                picks[i] = id;
            }
            return picks;
        }

        void SpawnMixed()
        {
            var rng = new System.Random(Environment.TickCount);
            var ids = MixedVehicles(rng);
            var carSlots = Enumerable.Range(0, AiCount).Where(n => CarData.AllowsVehicle(ids[n])).ToArray();
            var planeSlots = Enumerable.Range(0, AiCount).Where(n => !CarData.AllowsVehicle(ids[n])).ToArray();
            if (carSlots.Length > 0) WithLap(CarLap, CarData, () => { spawnLap = CarLap; SpawnDrivers(carSlots, ids); });
            else WithLap(CarLap, CarData, BuildAiRoad);   // (HIGH MODE's bumpers and the barriers read the car's road)
            if (planeSlots.Length > 0) WithLap(AirLap, AirData, () => { spawnLap = AirLap; SpawnPilots(planeSlots, ids); });
            spawnLap = null;
            Log($"mixed field: {carSlots.Length} AI cars on the road lap, {planeSlots.Length} AI planes on the rings ({string.Join(", ", ids)})");
            aiActive = true; nextEstimateSample = 0f;
        }
        RaceRoute spawnLap;   // the lap the spawners give their racers (null: the race's)

        // the air start (route air_start): a plane waits in the air on the lead-in, held, and is let go at GO at AirStartSpeed along the line
        public bool AirStartOf(RaceEntrant e) { var r = LapOf(e); return r != null && r.Air && r.Data != null && r.Data.air_start; }
        public void ReleaseAirStartOf(RaceEntrant e) => ReleaseAirStart(e);   // (PlanePilot at GO)
        void ReleaseAirStart(RaceEntrant e)
        {
            if (!e.Car || !AirStartOf(e)) return;
            var plane = e.Car.GetComponent<PlaneFlight>(); if (!plane || !plane.enabled) return;
            var r = LapOf(e); float s = r.LapStationOf(e.Car.Body.position); var a = r.PointAt(r.GateS + s - 10f, out _); var b = r.PointAt(r.GateS + s + 10f, out _);
            var d = b - a; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, pitch = Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            e.Car.Body.isKinematic = false;
            plane.AirStart(e.Car.Body.position, yaw, pitch, AirStartSpeed);
            if (e.IsPlayer) NotifyTeleported(); else e.NotifyTeleported();
        }

        // the combined board (cars and planes): every lap and race of a Wings & Wheels race also goes there
        string CombinedCategory => Twin == null ? null : RecordBoards.Category((HighFor(1) ? HighMode.RecordPrefix : "") + CombinedName, RecordDirection, Route.Laps);
    }
}
