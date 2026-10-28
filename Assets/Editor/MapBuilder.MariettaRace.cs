using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 78 (B, Dan: "crossing the train tracks which can lead to having to beat the train, jump over or get stuck waiting for it"): the Marietta Square race's
// world pieces from Data/tracks/marietta_square_race.json (python Tools/mapgen/make_marietta_route.py): the track crew's ballast piled between Mill
// Street's north curb and the shop fronts east of the tracks (a real jump over both tracks and a train on them), the spoil pile beside the road west of
// them its landing (a landing zone over it). Built as the other races' rock heaps (MapBuilder.BigJumps.cs); each pile's top set from its ground here
// (lip_y = the ground under the lip in this build + its height: the railroad's grading moved the ground near the tracks). World, not game layer.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class MariettaRaceFile { public CtBigJump[] big_jumps; public CtZone[] landing_zones; }
        const string MariettaRaceFile_ = "Data/tracks/marietta_square_race.json";

        static void BuildMariettaRace(TerrainGrid terrain, Materials mats, Transform world)
        {
            var path = Path.Combine(ProjectRoot, MariettaRaceFile_); if (!File.Exists(path)) return;
            var d = JsonUtility.FromJson<MariettaRaceFile>(File.ReadAllText(path)); if (d?.big_jumps == null) return;
            var root = new GameObject("Marietta race").transform; root.SetParent(world, false);
            foreach (var b in d.big_jumps) if (b.height > 0f) b.lip_y = Ground(terrain, b.lip[0], b.lip[1]) + b.height;
            BuildBigJumps(terrain, mats, root, d.big_jumps, "Marietta race jumps");
            if (d.landing_zones != null)
                foreach (var z in d.landing_zones) { var lz = new GameObject("Landing zone " + z.id); lz.transform.SetParent(root, false); lz.AddComponent<LandingZone>().outline = ToPoints(z.poly).ToArray(); }
            Debug.Log($"[Round78] B: the Marietta Square race's jump: {d.big_jumps.Length} piles, {d.landing_zones?.Length ?? 0} landing zones");
        }
    }
}
