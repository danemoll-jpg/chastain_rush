using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 48: the Free Roam pause menu's GOOSE EGGS page (Racer's pause total and its Exploration page's restart): the count and the tiers,
    // the areas one at a time (left / right, with where each one is), the map opened on that area, and a restart of the hunt (confirmed;
    // the tiers reached, the map's fog and places and every other save are kept). The pause menu's summary line carries the count.
    public sealed partial class GameMenus
    {
        int eggArea;
        string EggLine => EggHunt.Instance ? $"  ·  Goose eggs {EggHunt.Instance.Found} / {EggHunt.Instance.Total}" : "";

        void RenderEggs()
        {
            var h = EggHunt.Instance;
            if (!h) { Clear("GOOSE EGGS", "No egg hunt in this build."); Row(0, "back", "Back", BackPage); return; }
            var areas = EggAreas.Of(h); eggArea = areas.Length == 0 ? 0 : (eggArea % areas.Length + areas.Length) % areas.Length;
            int next = h.tiers.Where(t => t > h.Found).DefaultIfEmpty(0).Min();
            string tiers = string.Join("  ·  ", h.tiers.Select((t, i) => h.TiersReached.Contains(t) ? $"{t} ✓" : $"{t}"));
            var a = areas.Length > 0 ? areas[eggArea] : null;
            Clear("GOOSE EGGS", $"Found {h.Found} / {h.Total}{(next > 0 ? $"   ·   next tier at {next}" : "   ·   every tier reached")}\nTiers: {tiers}   (what each one unlocks comes later)" +
                                (a != null ? $"\n{a.name}: {a.Count(h)}{(a.Done(h) ? "  ✓" : "")}.  {a.direction}." : "") + (h.Error != null ? "\n" + h.Error : ""));
            int n = 0;
            if (a != null) Step(n++, "egg-area", $"{a.name}   {a.Count(h)}   ·   {eggArea + 1} / {areas.Length}", d => { eggArea += d; Show(); });
            Row(n++, "egg-map", "SHOW ON THE MAP", () => { int i = eggArea; CloseMenu(); var m = WorldMap.Instance; if (m) { m.Open(); m.SelectArea(i); } });
            Row(n++, "egg-restart", "RESTART THE HUNT", () => Confirm("RESTART THE EGG HUNT?", "Every egg goes back out to find again. Tiers reached, the map and all other saves are kept.", "RESTART", () => { h.Restart(); Show(); }));
            Row(n++, "back", "Back", BackPage);
        }
    }
}
