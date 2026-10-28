using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 78 (A): one level crossing of the railroad (every track's crossing of one road), built by the map (MapBuilder.Train.cs). RailTrains says when a
    // train comes (Closed: from 26 s before it reaches the road to when its last car has cleared it); the lamps flash from then, the arms come down 4 s later
    // and are down 14 s before it arrives, and go up when it has gone; the bell rings while the lamps flash. World piece: the trains are world too.
    public sealed class RailCrossing : MonoBehaviour
    {
        public Vector3 center, roadDir;
        public float half, spread;
        public int[] lines;          // railroad.json's lines that cross the road here
        public Transform[] arms;     // the gate arms (their hinge's +X toward the road's middle when down; local rotation Z 90 = up)
        public Renderer[] lamps;     // pairs per side: a0, a1, b0, b1

        public bool Closed { get; set; }
        public float ClosedFor { get; private set; }
        public float ArmAngle { get; private set; } = 90f;   // checks
        static Material on, off; AudioSource bell; float flash;

        void Start()
        {
            if (on == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                on = new Material(sh) { name = "Crossing lamp on", color = new Color(1f, .12f, .06f) }; on.SetColor("_BaseColor", new Color(1f, .12f, .06f));
                off = new Material(sh) { name = "Crossing lamp off", color = new Color(.22f, .03f, .02f) }; off.SetColor("_BaseColor", new Color(.22f, .03f, .02f));
            }
            foreach (var l in lamps) if (l) l.sharedMaterial = off;
        }

        void Update()
        {
            ClosedFor = Closed ? ClosedFor + Time.deltaTime : 0f;
            // the arms: down 4 s after the lamps start (8 s to come down), up at 15 degrees a second once the train has gone
            float want = Closed && ClosedFor > 4f ? 0f : 90f;
            ArmAngle = Mathf.MoveTowards(ArmAngle, want, (want < ArmAngle ? 11.25f : 15f) * Time.deltaTime);
            foreach (var a in arms) if (a) a.localRotation = Quaternion.Euler(0f, 0f, ArmAngle);
            bool lit = Closed || ArmAngle < 89.5f;
            if (lit) flash += Time.deltaTime; else flash = 0f;
            bool phase = Mathf.Repeat(flash, 1.1f) < .55f;
            for (int i = 0; i < lamps.Length; i++) if (lamps[i]) lamps[i].sharedMaterial = lit && ((i % 2 == 0) == phase) ? on : off;
            // the bell, only near the camera
            var cam = Camera.main; bool near = cam && (cam.transform.position - center).sqrMagnitude < 300f * 300f;
            if (lit && near)
            {
                if (!bell)
                {
                    var bo = new GameObject("Crossing bell"); bo.transform.SetParent(transform, false); bo.transform.position = center + Vector3.up * 2.5f;
                    bell = bo.AddComponent<AudioSource>(); bell.clip = RailSounds.Bell; bell.loop = true; bell.spatialBlend = 1f; bell.minDistance = 8f; bell.maxDistance = 220f;
                    bell.rolloffMode = AudioRolloffMode.Linear; bell.dopplerLevel = 0f; SoundMix.Track(bell, .55f, SoundMix.Kind.Effects);
                }
                if (!bell.isPlaying) bell.Play();
            }
            else if (bell && bell.isPlaying) bell.Stop();
        }
    }
}
