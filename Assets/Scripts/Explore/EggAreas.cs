using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 48: the egg hunt's areas (EggHunt.Site.area) as places on the map. Ported from Racer's AcornAreas (0.93 Part C): an area's eggs
    // can be far apart, so an area is drawn as one or more rounded regions: eggs within Join meters of each other share one, the convex hull
    // of its eggs grown by Margin all round; a lone egg gets a wider circle whose center is moved off the egg, so no region's middle gives a
    // spot away. Each area's direction in words comes from Data/explore/eggs.json (make_eggs.py). Racer's place names are left behind (the
    // map picture has the street names' places already).
    public static class EggAreas
    {
        public const float Margin = 60, Join = 150, Lone = 85, LoneShift = 40;
        public sealed class Region { public Vector3[] outline; public Vector3 label; public Bounds bounds; public EggHunt.Site[] sites; public bool named; }
        public sealed class Area
        {
            public string name, direction; public Color tint; public EggHunt.Site[] sites; public Region[] regions; public Bounds bounds;
            public int Found(EggHunt h) => sites.Count(s => h.Discovered(s.id));
            public int Total => sites.Length;
            public string Count(EggHunt h) => $"{Found(h)} / {Total}";
            public bool Done(EggHunt h) => Found(h) >= Total;
            public bool Contains(Vector3 p) => regions.Any(r => Inside(r.outline, p));
        }
        static readonly Color[] Tints =
        {
            new Color(.55f, .9f, .35f), new Color(1f, .6f, .3f), new Color(.35f, .75f, 1f), new Color(.9f, .55f, 1f), new Color(1f, .45f, .38f),
            new Color(1f, .85f, .3f), new Color(.4f, 1f, .85f), new Color(.75f, .75f, 1f), new Color(1f, .65f, .8f), new Color(.7f, .95f, .55f), new Color(.95f, .8f, .6f),
        };
        static bool Inside(Vector3[] o, Vector3 p)
        {
            for (int i = 0; i < o.Length; i++) { var a = o[i]; var b = o[(i + 1) % o.Length]; if ((b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x) < 0) return false; }
            return true;
        }

        static EggHunt built; static Area[] areas = new Area[0];
        public static Area[] Of(EggHunt h)
        {
            if (!h) return new Area[0];
            if (built == h && areas.Length > 0) return areas;
            built = h;
            var order = h.areas.Select(a => a.name).ToList();
            areas = h.sites.GroupBy(s => s.area).OrderBy(g => order.IndexOf(g.Key) < 0 ? 99 : order.IndexOf(g.Key))
                     .Select((g, i) => Build(g.Key, g.ToArray(), h.areas.FirstOrDefault(a => a.name == g.Key)?.direction ?? "", Tints[i % Tints.Length])).ToArray();
            return areas;
        }
        // the area a point is in (the egg's own area when it is in two: regions can overlap)
        public static Area At(EggHunt h, Vector3 p) => Of(h).FirstOrDefault(a => a.sites.Any(s => (s.position - p).sqrMagnitude < 1f)) ?? Of(h).FirstOrDefault(a => a.Contains(p));

        static Area Build(string name, EggHunt.Site[] sites, string direction, Color tint)
        {
            var groups = new List<List<EggHunt.Site>>();
            foreach (var s in sites)
            {
                var near = groups.Where(g => g.Any(o => Flat(o.position - s.position) < Join)).ToList();
                var merged = new List<EggHunt.Site> { s }; foreach (var g in near) { merged.AddRange(g); groups.Remove(g); }
                groups.Add(merged);
            }
            var regions = groups.Select(MakeRegion).ToArray();
            regions.OrderByDescending(r => r.sites.Length).First().named = true;
            var bounds = regions[0].bounds; foreach (var r in regions) bounds.Encapsulate(r.bounds);
            return new Area { name = name, sites = sites, regions = regions, bounds = bounds, direction = direction, tint = tint };
        }
        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
        static Region MakeRegion(List<EggHunt.Site> group)
        {
            var pts = group.Select(s => new Vector2(s.position.x, s.position.z)).ToList(); float grow = Margin;
            if (pts.Count == 1)
            {
                int h = 17; foreach (char ch in group[0].id) h = h * 31 + ch; float a = ((h & 0x7fffffff) % 360) * Mathf.Deg2Rad; pts[0] += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * LoneShift; grow = Lone;
            }
            var outline = new List<Vector3>(); const int steps = 72;
            for (int k = 0; k < steps; k++)
            {
                float a = k * Mathf.PI * 2 / steps; var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var far = pts.OrderByDescending(p => Vector2.Dot(p, u)).First() + u * grow; outline.Add(new Vector3(far.x, 0, far.y));
            }
            var bounds = new Bounds(outline[0], Vector3.zero); foreach (var p in outline) bounds.Encapsulate(p);
            var middle = pts.Aggregate(Vector2.zero, (x, y) => x + y) / pts.Count;
            var label = new Vector3(middle.x, 0, Mathf.Lerp(middle.y, bounds.max.z, .55f));
            return new Region { outline = outline.ToArray(), label = label, bounds = bounds, sites = group.ToArray() };
        }
    }
}
