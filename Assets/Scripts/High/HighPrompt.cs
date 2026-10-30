using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 84 (A5): Racer 1.2's HighPrompt, ported. "Would you like to play HIGH MODE?": in normal mode, one player, 3 wrecks or resets within
    // 2 minutes, or 2 in one lap of a race, pauses and asks (GameMenus.AskHighMode). A wreck: the vehicle on its roof or side for 1.2 s (a bike's
    // rider down, a car upside down); a reset: R / Y or a crash's reset (VehicleRespawn.ByReset), not the grid or map travel. A reset right after a
    // wreck is the same mishap. Not in split screen, the Stunt event (a bail is part of it), a replay, Trailer Mode or a check that has not asked
    // for it. No: not asked again this session (HighMode.Declined).
    public sealed class HighPrompt : MonoBehaviour
    {
        public const int InTwoMinutes = 3, InOneLap = 2; public const float Window = 120, SameMishap = 5;
        GameMenus menus; ArcadeVehicle car; VehicleRespawn respawn;
        readonly List<float> times = new(); float lastWreck = -100, overturned; bool down; int lap = -1, lapCount;
        public static int Asked { get; private set; }   // checks
        public static int Mishaps { get; private set; }   // checks
        public void Initialize(GameMenus owner, ArcadeVehicle vehicle)
        {
            menus = owner; car = vehicle; respawn = car ? car.GetComponent<VehicleRespawn>() : null;
            if (respawn) respawn.Respawned += OnReset;
        }
        void OnDestroy() { if (respawn) respawn.Respawned -= OnReset; }
        bool Watching
        {
            get
            {
                if (!menus || !car || HighMode.Declined || HighMode.Now || GameMenus.Open) return false;
                if (SplitScreen.Active || StuntEvent.Current || ReplayViewer.Active || TrailerMode.Active || WorldMap.IsOpen) return false;
                var r = RaceController.Instance; if (r && (r.State == RaceController.Stage.Countdown || r.State == RaceController.Stage.Finished)) return false;
                return true;
            }
        }
        void OnReset()
        {
            if (!respawn.ByReset || !Watching) return;
            if (Time.time - lastWreck < SameMishap) return;   // the reset after a wreck: one mishap
            Mishap(false);
        }
        void Mishap(bool wreck)
        {
            if (wreck) lastWreck = Time.time;
            times.Add(Time.time); lapCount++; Mishaps++;
            Debug.Log($"[High] mishap ({(wreck ? "wreck" : "reset")}): {times.Count} in 2 minutes, {lapCount} this lap");
        }
        void Update()
        {
            if (!car) return;
            bool watching = Watching;
            // a wreck: on its roof or side for 1.2 s (once per overturn)
            bool over = car.transform.up.y < .35f && car.Body.linearVelocity.magnitude < 6 && !car.Body.isKinematic;
            overturned = over ? overturned + Time.deltaTime : 0; if (!over) down = false;
            if (overturned > 1.2f && !down) { down = true; if (watching) Mishap(true); }
            var r = RaceController.Instance; bool racing = r && r.State == RaceController.Stage.Racing;
            if (!watching) { if (!racing && !GameMenus.Open && (!r || r.State == RaceController.Stage.Free) && HighMode.Now) { times.Clear(); lapCount = 0; } return; }
            int laps = racing ? r.CompletedLaps : -1; if (laps != lap) { lap = laps; lapCount = 0; }
            times.RemoveAll(t => Time.time - t > Window);
            if (times.Count >= InTwoMinutes || (racing && lapCount >= InOneLap)) { times.Clear(); lapCount = 0; Asked++; menus.AskHighMode(); }
        }
    }
}
