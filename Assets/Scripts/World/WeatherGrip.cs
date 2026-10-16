using UnityEngine;

namespace Racer
{
    // 0.97 Part B: weather changes the grip. Rain and snow multiply the tyres' grip by the surface under each wheel (ArcadeVehicle
    // reads it from its suspension contacts): paved / dirt / grass, and ice (the frozen lake and pool in Snow). Clear is unchanged;
    // so is everything while Settings > Gameplay > "Weather affects grip" is Off (the 0.95 behaviour). Braking distance and cornering
    // speed follow from the grip; top speed is not reduced.
    //
    // Chastain Rush, Round 35 (Part C): Racer's WeatherGrip, ported. Edits (commented "Chastain Rush" in place): the setting is
    // GameSettings'; the race's weather is the race setup's (GameMenus' race settings); paved is a road of the map (RoadLookup,
    // within its half width and a metre: the car drives on the terrain, roads have no colliders), a bridge deck, a ramp or a pipe;
    // everything else is grass (this map's trails are not in the road list: dirt and grass differ by 0.03); no rain or snow falls
    // underground, so the pipe network, its junctions and the hub keep full grip. No split-screen or campaign.
    public static class WeatherGrip
    {
        public enum Surface { Paved, Dirt, Grass, Ice }
        // multipliers on the tyres' grip: [Paved, Dirt, Grass, Ice]
        public static readonly float[] Rain = { .85f, .78f, .75f, .35f }, Snow = { .70f, .65f, .62f, .35f };
        static int frame = -1; static Weather now; static bool enabled = true;
        public static bool Enabled { get { Refresh(); return enabled; } }
        public static Weather Now { get { Refresh(); return now; } }
        // the weather that grips the road now (Clear with the setting Off)
        public static Weather InEffect { get { Refresh(); return enabled ? now : Weather.Clear; } }
        static void Refresh()
        {
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            enabled = GameSettings.Settings.weatherGrip;   // Chastain Rush: GameSettings (Racer: flow.Save.Settings)
            var look = WorldLook.Current; now = Weather.Clear;
            if (look) now = look.Mode == "Free Roam" ? look.RoamWeather : look.Mode == "Race" ? look.RaceWeather : Weather.Clear;
        }
        public static float Scale(Weather weather, Surface surface) => weather == Weather.Clear ? 1 : (weather == Weather.Rain ? Rain : Snow)[(int)surface];
        // the record-key suffix that keeps wet and snow times off the dry boards: "" in Clear and with the setting Off, "-rain" or
        // "-snow" otherwise. `chosen` is the race's own weather (the race setup's), so it holds after the finish too.
        public static string RecordTag(Weather chosen) { Refresh(); return !enabled ? "" : chosen == Weather.Rain ? "-rain" : chosen == Weather.Snow ? "-snow" : ""; }
        public static Weather ChosenFor() => (Weather)Mathf.Clamp(ChastainRush.GameMenus.Current.weather, 0, 2);   // Chastain Rush: the race setup's weather
        // the grip under one wheel's contact
        public static float ForHit(Weather weather, Collider collider, Vector3 point)
        {
            if (collider && collider.name.StartsWith("Frozen water", System.StringComparison.Ordinal)) return Scale(weather, Surface.Ice);
            if (Underground(point)) return 1;   // Chastain Rush: nothing falls in the pipe network
            return Scale(weather, SurfaceAt(point, collider));
        }

        // ---------- Chastain Rush: paved / grass by the map's roads (RoadLookup) ----------
        public static Surface SurfaceAt(Vector3 p, Collider collider = null)
        {
            if (collider && !(collider is TerrainCollider)) return Surface.Paved;   // a bridge deck, a ramp, a lot's kerb, a pipe's floor
            var roads = ChastainRush.RoadLookup.Instance;
            if (roads && roads.Nearest(p, out _, out float distance, out float halfWidth) && distance <= halfWidth + 1f) return Surface.Paved;
            return Surface.Grass;
        }
        // more than 2 m under the terrain: in a pipe, a junction or the hub (their open cuts at the mouths are not)
        static bool Underground(Vector3 p)
        {
            foreach (var t in Terrain.activeTerrains)
            {
                if (!t || !t.terrainData) continue; var o = t.GetPosition(); var size = t.terrainData.size;
                if (p.x < o.x || p.z < o.z || p.x > o.x + size.x || p.z > o.z + size.z) continue;
                return t.SampleHeight(p) + o.y - p.y > 2f;
            }
            return false;
        }
    }
}
