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

        // Round 81 (E, Dan's BUG-002: the arm left standing straight up in the road when its post was broken): when the gear beside an arm's hinge
        // breaks, the arm comes off and lies on the verge along the road, away from the tracks (never in the lane); it goes back with the gear
        bool[] fallen; Vector3[] hingePos, brokeAt; Quaternion[] hingeRot;
        void OnEnable() { FenceChunk.PanelBroken += Broke; FenceChunk.AllRestored += Mend; FenceChunk.Restored += MendChunk; }
        void OnDisable() { FenceChunk.PanelBroken -= Broke; FenceChunk.AllRestored -= Mend; FenceChunk.Restored -= MendChunk; }
        void Remember()
        {
            if (fallen != null) return;
            fallen = new bool[arms.Length]; hingePos = new Vector3[arms.Length]; brokeAt = new Vector3[arms.Length]; hingeRot = new Quaternion[arms.Length];
            for (int k = 0; k < arms.Length; k++) if (arms[k]) { hingePos[k] = arms[k].parent.position; hingeRot[k] = arms[k].parent.rotation; }
        }
        void Broke(FenceChunk c, int panel, Vector3 at)
        {
            if ((at - center).sqrMagnitude > 60f * 60f) return; Remember();
            for (int k = 0; k < arms.Length; k++)
            {
                if (!arms[k] || fallen[k]) continue; var h = hingePos[k];
                if (new Vector2(h.x - at.x, h.z - at.z).magnitude > 3.5f) continue;
                fallen[k] = true; brokeAt[k] = at;
                var away = new Vector3(h.x - center.x, 0f, h.z - center.z); away = Vector3.Project(away, roadDir); if (away.sqrMagnitude < 1e-4f) away = roadDir; away.Normalize();
                float gy = Physics.Raycast(h + Vector3.up * 2f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : h.y - 1.25f;
                arms[k].parent.SetPositionAndRotation(new Vector3(h.x, gy + .08f, h.z), Quaternion.LookRotation(Vector3.Cross(away, Vector3.up), Vector3.up));
                arms[k].localRotation = Quaternion.identity;
            }
        }
        void Mend() { if (fallen == null) return; for (int k = 0; k < arms.Length; k++) if (fallen[k] && arms[k]) { fallen[k] = false; arms[k].parent.SetPositionAndRotation(hingePos[k], hingeRot[k]); } }
        void MendChunk(FenceChunk c) { if (fallen == null) return; for (int k = 0; k < arms.Length; k++) if (fallen[k] && !FenceChunk.BrokenAt(brokeAt[k])) { fallen[k] = false; arms[k].parent.SetPositionAndRotation(hingePos[k], hingeRot[k]); } }

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

        // Round 79 (A): in a replay the lamps and gates as they were (RailTrains.ReplayAt): closed for closedFor s, or open again 'since' s after a
        // closing of closedLen s; Live hands them back to the running game
        bool replayed; float shownFlash;
        public void ShowAt(bool closed, float closedFor, float since, float closedLen)
        {
            replayed = true;
            float ArmAfter(float c) => c <= 4f ? 90f : Mathf.Max(0f, 90f - (c - 4f) * 11.25f);
            ArmAngle = closed ? ArmAfter(closedFor) : Mathf.Min(90f, ArmAfter(closedLen) + since * 15f);
            ClosedFor = closed ? closedFor : 0f;
            shownFlash = closed ? closedFor : closedLen + since;
        }
        public void Live() { replayed = false; }

        void Update()
        {
            if (!replayed)
            {
                ClosedFor = Closed ? ClosedFor + Time.deltaTime : 0f;
                // the arms: down 4 s after the lamps start (8 s to come down), up at 15 degrees a second once the train has gone
                float want = Closed && ClosedFor > 4f ? 0f : 90f;
                ArmAngle = Mathf.MoveTowards(ArmAngle, want, (want < ArmAngle ? 11.25f : 15f) * Time.deltaTime);
            }
            for (int k = 0; k < arms.Length; k++) if (arms[k] && (fallen == null || !fallen[k])) arms[k].localRotation = Quaternion.Euler(0f, 0f, ArmAngle);
            bool lit = Closed || ArmAngle < 89.5f;
            if (replayed) flash = shownFlash; else if (lit) flash += Time.deltaTime; else flash = 0f;
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
