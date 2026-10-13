using UnityEngine;

namespace ChastainRush
{
    // Round 23 follow-ups (the network's frame-rate plan, Docs/PIPE-NETWORK-PROPOSAL.md): the underground pipe network
    // (MapBuilder.Network.cs) is cut into cells (about 60 m of pipe, a chamber, the hub, a manhole, a glass stretch). A cell's
    // meshes, lamps, streams, waterfalls and fish are switched on only while the camera is within reach of it (in it, the next
    // one, or looking into a mouth from the surface); otherwise none of it is drawn, lit or run. Colliders and the DrainPipe
    // rules (resets, wall riding) always stay on: nothing a vehicle can touch ever goes away.
    public sealed class NetworkCells : MonoBehaviour
    {
        public Transform[] roots;
        public Bounds[] bounds;
        public string[] labels; public Vector3[] labelAt;   // the map's names underground: the junctions, the hub, manholes, exits
        public const float Reach = 70f;       // on within this of the camera (m, to the cell's box)
        const float Every = 0.2f;
        bool[] on; float next;
        public static int CellsOn { get; private set; }
        public static NetworkCells Instance { get; private set; }

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        void Start()
        {
            on = new bool[roots.Length];
            for (int i = 0; i < roots.Length; i++) { on[i] = true; Set(i, false); }
            next = 0f;
        }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Every;
            var cam = Camera.main; if (!cam) return;
            var p = cam.transform.position; int count = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                bool want = bounds[i].SqrDistance(p) < Reach * Reach;
                if (want != on[i]) Set(i, want);
                if (want) count++;
            }
            CellsOn = count;
        }

        // check runs: everything on (or back to the camera's rule)
        public void ForceAll(bool all) { for (int i = 0; i < roots.Length; i++) Set(i, all || bounds[i].SqrDistance(Camera.main ? Camera.main.transform.position : Vector3.zero) < Reach * Reach); }

        void Set(int i, bool v)
        {
            on[i] = v; var r = roots[i]; if (!r) return;
            foreach (var x in r.GetComponentsInChildren<Renderer>(true)) x.enabled = v;
            foreach (var x in r.GetComponentsInChildren<Light>(true)) x.enabled = v;
            foreach (var x in r.GetComponentsInChildren<AudioSource>(true)) { x.enabled = v; }
            foreach (var x in r.GetComponentsInChildren<PipeStream>(true)) x.enabled = v;
            foreach (var x in r.GetComponentsInChildren<NetworkWaterfall>(true)) x.enabled = v;
            foreach (var x in r.GetComponentsInChildren<PipeAquarium>(true)) x.enabled = v;
            foreach (var x in r.GetComponentsInChildren<ParticleSystem>(true)) { if (v) x.Play(); else x.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        }
    }
}
