using System;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 36 (Part D): the hydrants tied to the underground (Dan picked these two, 10:57: "1 and 2 are kind of what I was thinking"):
    //   rigs    a hydrant beside a street manhole (MH1 to MH13): knock it over and that manhole's cover blows off (ManholeLid.BlowOpen) and the way
    //           down the riser into the network is open for a while (a reset closes it). Nothing marks which hydrants are rigged: the manhole beside
    //           it is the only clue (found, not signposted).
    //   spouts  a hydrant up the street from a storm grate: knock it over and the grate's geyser erupts (Geyser).
    // World: these links (made by the map build, MapBuilder.Hydrants.cs). Their effects run in Free Roam only for now (Dan, 11:01: "free roam only for
    // now, but this concept could be used as a shortcut idea in races"): each link has `inRaces`, off; a race route could switch one on (a rigged
    // hydrant opening a manhole into a pipe shortcut, a geyser launching over something). In a race the hydrants break as before (HydrantWater).
    public sealed class HydrantLinks : MonoBehaviour
    {
        [Serializable] public struct Rig { public FenceChunk chunk; public int panel; public ManholeLid lid; public bool inRaces; }
        [Serializable] public struct Spout { public FenceChunk chunk; public int panel; public Geyser geyser; public bool inRaces; }
        public Rig[] rigs = new Rig[0];
        public Spout[] spouts = new Spout[0];
        public const float OpenSeconds = 45f;
        public static HydrantLinks Instance { get; private set; }
        VehicleRespawn respawn;

        void OnEnable() { Instance = this; FenceChunk.PanelBroken += Broken; }
        void OnDisable() { FenceChunk.PanelBroken -= Broken; if (Instance == this) Instance = null; }
        void Start()
        {
            var sw = FindAnyObjectByType<VehicleSwitch>(); respawn = sw ? sw.GetComponent<VehicleRespawn>() : null;
            if (respawn) respawn.Respawned += CloseAll;
        }
        void OnDestroy() { if (respawn) respawn.Respawned -= CloseAll; }
        static bool Allowed(bool inRaces) { var r = RaceController.Instance; return inRaces || !r || r.State == RaceController.Stage.Free; }
        void Broken(FenceChunk chunk, int panel, Vector3 at)
        {
            foreach (var r in rigs) if (r.chunk == chunk && r.panel == panel && r.lid && Allowed(r.inRaces)) r.lid.BlowOpen(OpenSeconds);
            foreach (var s in spouts) if (s.chunk == chunk && s.panel == panel && s.geyser && Allowed(s.inRaces)) s.geyser.Erupt();
        }
        // a reset closes the blown covers (Dan: "The cover is back after a while or a reset")
        void CloseAll() { foreach (var r in rigs) if (r.lid && r.lid.Open && !(r.lid.lid && Vector3.Distance(r.lid.lid.position, respawn.transform.position) < 6f)) r.lid.CloseNow(); }
    }
}
