using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 78 (A): one car of a running train (RailTrains). Hitting it is a crash: the player's vehicle is put back after a moment (VehicleRespawn.ResetSoon,
    // as R does), an AI racer recovers (RoadDriver.ForceRecover), a traffic car is moved away when nobody sees it (TrafficDriver.Recycle). The train goes on.
    public sealed class TrainCar : MonoBehaviour
    {
        public static int Hits;   // checks
        void OnCollisionEnter(Collision c)
        {
            var rb = c.rigidbody; if (!rb) return;
            var car = rb.GetComponent<ArcadeVehicle>(); if (!car) return;
            Hits++;
            var rd = rb.GetComponent<RoadDriver>(); if (rd && rd.enabled) { rd.ForceRecoverSoon(1.2f); return; }
            var td = rb.GetComponent<TrafficDriver>(); if (td && td.enabled) { td.Recycle(); return; }
            var vr = rb.GetComponent<VehicleRespawn>(); if (vr) vr.ResetSoon(1.2f);
        }
    }
}
