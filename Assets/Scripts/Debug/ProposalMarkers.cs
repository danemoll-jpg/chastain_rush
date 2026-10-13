using System;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 23, Part B: back for the underground pipe network PROPOSAL (Tools/mapgen/pipe_network_proposal.py; each line its own
    // color, the labeled points also on the map, a legend). Before: Round 18, items 2b and 2c; Round 20, item 4 (the Noonday Creek
    // shortcut candidates N1 to N5) and Round 21, Part C (the reverse Noonday Creek candidates RA to RC). TEMPORARY: remove
    // this, MapBuilder.Proposals.cs, Data/proposals/ and Assets/HandMade/Proposals/ when Dan has chosen. Markers only, for Dan
    // to drive round and judge. Posts, beams and floating labels with no colliders (nothing a vehicle can touch) and no change
    // to the ground. Built by the map build, hidden at start; shown from the debug menu (GameMenus: "Proposal markers" and
    // "Go to"). A check run can show them with -proposalMarkers 1. Round 20: the lines and their names are drawn on the map
    // (WorldMap.Proposals) whether the markers are shown or not.
    public sealed class ProposalMarkers : MonoBehaviour
    {
        public static ProposalMarkers Instance { get; private set; }

        public GameObject[] groups = new GameObject[0];
        public Transform[] boards = new Transform[0];
        public string[] startNames = new string[0];
        public Vector3[] starts = new Vector3[0];   // x, z, yaw
        // Round 20: the proposed lines for the map (names, the first point of each in linePoints, all points)
        public string[] lineNames = new string[0];
        public int[] lineStarts = new int[0];
        public Vector3[] linePoints = new Vector3[0];
        public Color lineColor = new Color(1f, 0.25f, 0.85f);
        public Color[] lineColors = new Color[0];   // Round 23: one per line (lineColor when missing)
        public float[] lineWidths = new float[0];
        public bool[] lineNamed = new bool[0];     // its name drawn on the map
        public string[] markNames = new string[0]; public Vector3[] markPoints = new Vector3[0]; public Color[] markColors = new Color[0];
        public string legend = "";

        public static readonly string[] ModeNames = { "Off", "Shown" };
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
            WorldMap.Proposals.Clear(); WorldMap.ProposalMarks.Clear(); WorldMap.ProposalLegend = legend;
            for (int i = 0; i < lineNames.Length; i++)
            {
                int a = lineStarts[i], b = i + 1 < lineStarts.Length ? lineStarts[i + 1] : linePoints.Length;
                var pts = new Vector3[b - a]; Array.Copy(linePoints, a, pts, 0, b - a);
                WorldMap.Proposals.Add(new WorldMap.Proposal { label = i < lineNamed.Length && !lineNamed[i] ? null : lineNames[i], line = pts,
                    color = i < lineColors.Length ? lineColors[i] : lineColor, width = i < lineWidths.Length ? lineWidths[i] : 3f });
            }
            for (int i = 0; i < markNames.Length; i++) WorldMap.ProposalMarks.Add(new WorldMap.ProposalMark { label = markNames[i], at = markPoints[i], color = markColors[i] });
        }
        void OnDestroy() { if (Instance == this) { Instance = null; WorldMap.Proposals.Clear(); WorldMap.ProposalMarks.Clear(); } }

        public void Step(int d) { Mode = (Mode + d + ModeNames.Length) % ModeNames.Length; Apply(); }

        void Apply() { foreach (var g in groups) if (g) g.SetActive(Mode == 1); }

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

        // free roam only: the player's vehicle to the start of a proposal (60 m before it on the lap), facing along it
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
