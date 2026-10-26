using UnityEngine;

// Round 72 (B3, a known fix found by Round 64's check: "drives with air over 0.15 s went up" after Round 64 C's junction levelling): the six junctions the
// jolt table still names with air over 0.15 s at 28 m/s, their ground blurred after the levelling (Round 55's blur), from Data/tracks/round72_junctions.json.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const string Round72JunctionsFile = "Data/tracks/round72_junctions.json";
        static void R72SmoothJunctions(TerrainGrid terrain)
        {
            string path = System.IO.Path.Combine(ProjectRoot, Round72JunctionsFile);
            if (!System.IO.File.Exists(path)) return;
            R55Blur(terrain, JsonUtility.FromJson<R55File>(System.IO.File.ReadAllText(path)));
        }
    }
}
