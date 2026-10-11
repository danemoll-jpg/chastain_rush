using System;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 18, items 2b and 2c. TEMPORARY: remove this, MapBuilder.Round18Proposals.cs, Data/proposals/ and
    // Assets/HandMade/Proposals/ when Dan has chosen. Markers only, for Dan to drive round and judge: the creek track proposals
    // A and B, the pipe, and the reverse Chastain Glen loop's shortcut candidates. Posts, beams and floating labels with no
    // colliders (nothing a vehicle can touch) and no change to the ground. Built by the map build, hidden at start; shown
    // from the debug menu (GameMenus: "Proposal markers" and "Go to"). A check run can show them with
    // -proposalMarkers <0..5>.
    public sealed class ProposalMarkers : MonoBehaviour
    {
        public static ProposalMarkers Instance { get; private set; }

        // the groups as the map build made them: A only, A and B (shared line and features), B only, the pipe, the reverse candidates
        public GameObject groupA, groupAB, groupB, groupPipe, groupReverse;
        public Transform[] boards = new Transform[0];
        public string[] startNames = new string[0];
        public Vector3[] starts = new Vector3[0];   // x, z, yaw

        public static readonly string[] ModeNames = { "Off", "A", "B", "A + B", "Reverse shortcuts", "All" };
        public int Mode { get; private set; }

        void Awake()
        {
            Instance = this;
            Mode = 0;
            if (CheckRun.Active)
            {
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i + 1 < args.Length; i++)
                    if (args[i] == "-proposalMarkers" && int.TryParse(args[i + 1], out int m)) Mode = Mathf.Clamp(m, 0, ModeNames.Length - 1);
            }
            Apply();
        }

        public void Step(int d) { Mode = (Mode + d + ModeNames.Length) % ModeNames.Length; Apply(); }

        void Apply()
        {
            bool a = Mode == 1 || Mode == 3 || Mode == 5, b = Mode == 2 || Mode == 3 || Mode == 5, r = Mode == 4 || Mode == 5;
            if (groupA) groupA.SetActive(a);
            if (groupB) groupB.SetActive(b);
            if (groupAB) groupAB.SetActive(a || b);
            if (groupPipe) groupPipe.SetActive(a || b);
            if (groupReverse) groupReverse.SetActive(r);
        }

        // the labels turn to face the camera (about the vertical only); their text is on the side that faces it
        void LateUpdate()
        {
            if (Mode == 0) return;
            var cam = Camera.main; if (!cam) return;
            var c = cam.transform.position;
            foreach (var t in boards)
            {
                if (!t || !t.gameObject.activeInHierarchy) continue;
                var d = t.position - c; d.y = 0f;
                if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d);
            }
        }

        // free roam only: the player's vehicle to the start of a proposal, facing along it
        public bool GoTo(int i)
        {
            if (i < 0 || i >= starts.Length) return false;
            var car = FindAnyObjectByType<ArcadeVehicle>();
            var respawn = car ? car.GetComponent<VehicleRespawn>() : null;
            if (!respawn) return false;
            var s = starts[i];
            respawn.PlaceAt(RaceRoute.Ground(s.x, s.y) + Vector3.up * 0.3f, s.z);
            return true;
        }
    }
}
