using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Round 78 (A, Dan: "would it be possible to build a running train?"). The railroad's world pieces for the running freight train (World/Rail/):
// - the level crossings grouped per road (every track's crossing of one road within 12 m is one crossing: Round 77 drew the second Marietta track's gear
//   between the tracks): on each side, on the right of the traffic arriving, the crossbuck post and the lamp bar (breakable as before), the gate's
//   mechanism, and apart from them the gate arm on its hinge and the two red lamps, driven by RailCrossing at run time (arms down and lamps flashing while
//   a train comes);
// - Assets/Generated/Resources/Railroad.json: every line's points with the bed's heights (the train runs on them) and the routes the trains run;
// - the train kit (inactive in the scene, the cars copied from it at run time): two locomotive liveries, boxcars, tank cars, covered hoppers, our own
//   shapes and plain colors (no railroad's name or logo). World pieces; the timetable is World/Rail/RailTrains.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // Round 78 (G, Round 77's BUG-003 x 1618.42 z -6110.74 on the Canton Road Connector: "a stray white block on the left"): OSM's hut 6854, 1.9 m by
        // 3.9 m at x 1590.5 z -6036.6 by the service road's turnaround, stood 11.4 m tall by the business dressing: left out
        static bool R78Gone(int i) => i == 6854;
        const string RailDataPath = GeneratedDir + "/Resources/Railroad.json";
        const float RailTop = .46f;   // the rails' top over the bed's profile (ballast .06, ties .18, rails .14 over the ties' top at .32)

        [Serializable] class RailOutLine { public int src; public float[] x, y, z; }
        [Serializable] class RailOutRoute { public string name; public int[] lines; public int[] dirs; }
        [Serializable] class RailOut { public RailOutLine[] lines; public RailOutRoute[] routes; }

        static int BuildRailCrossings(TerrainGrid t, Materials mats, Transform root)
        {
            // groups: crossings within 12 m of each other whose roads run the same way (two roads crossing at one spot keep their own gear)
            var groups = new List<List<int>>();
            for (int i = 0; i < RailCrossings.Count; i++)
            {
                var c = RailCrossings[i]; List<int> into = null;
                foreach (var g in groups)
                {
                    var c0 = RailCrossings[g[0]];
                    if ((c0.at - c.at).magnitude < 12f && Mathf.Abs(Vector2.Dot(c0.road, c.road)) > .9f) { into = g; break; }
                }
                if (into == null) groups.Add(new List<int> { i }); else into.Add(i);
            }
            var gear = new Breakables(); var white = new Color(.93f, .93f, .90f); var red = new Color(.78f, .10f, .08f); var dark = new Color(.10f, .10f, .11f);
            var lampOff = new Color(.30f, .04f, .03f);
            var gates = new GameObject("Railroad crossings").transform; gates.SetParent(root, false);
            int gi = 0;
            foreach (var g in groups)
            {
                var members = g.Select(k => RailCrossings[k]).ToList();
                var at = new Vector2(members.Average(m => m.at.x), members.Average(m => m.at.y)); var road = members[0].road;
                float half = members.Max(m => m.half), spread = members.Max(m => Mathf.Abs(Vector2.Dot(m.at - at, road)));
                var go = new GameObject($"Railroad crossing {gi}"); go.transform.SetParent(gates, false);
                var rc = go.AddComponent<RailCrossing>();
                rc.center = new Vector3(at.x, t.Height(at.x, at.y), at.y); rc.roadDir = new Vector3(road.x, 0f, road.y); rc.half = half; rc.spread = spread;
                rc.lines = members.Select(m => RailPlans[m.line].src).Distinct().ToArray();
                var arms = new List<Transform>(); var lamps = new List<Renderer>();
                foreach (float s in new[] { -1f, 1f })
                {
                    var come = road * s; var right = new Vector2(come.y, -come.x);
                    var p = at - come * (4.2f + spread) + right * (half + .9f); float gy = t.Height(p.x, p.y);
                    var f = MakeFrame(new Vector3(p.x, gy, p.y), -come, 1f);
                    var mb = gear.Begin(p);
                    // Round 81 (E, Dan's BUG-002 at Whitlock: "what is this?"): the gear breaks into pieces that look like what they were: the post in two
                    // lengths, the crossbuck, the lamp bar with its hoods, the mechanism box (one piece each, each coming to rest at its own height); the
                    // crossbuck's boards face the traffic (they were laid edge-on: thin slivers standing, slabs flying)
                    gear.Piece(() => Post(mb, new Vector3(p.x, gy - .1f, p.y), 2.2f, .16f, white), gy + .1f);
                    gear.Piece(() => Post(mb, new Vector3(p.x, gy + 2.1f, p.y), 2.1f, .16f, white), gy + .1f);
                    gear.Piece(() =>
                    {
                        var c0 = f.P(0f, 3.75f, .12f);
                        foreach (float ang in new[] { 45f, -45f }) { var dir = Quaternion.AngleAxis(ang, f.F) * f.R; Beam(mb, c0 - dir * 1.1f, c0 + dir * 1.1f, .26f, .03f, white, Vector3.Cross(f.F, dir)); }
                    }, gy + .05f);
                    gear.Piece(() =>
                    {
                        // the lamp bar and hoods (the lamps themselves are the run-time pieces below)
                        BoxF(mb, f, -.75f, .75f, 2.55f, 2.68f, .05f, .14f, dark);
                        foreach (float u in new[] { -.55f, .55f }) { Cyl(mb, f.P(u, 2.35f, .18f), .02f, .25f, .25f, dark, 10); BoxF(mb, f, u - .24f, u + .24f, 2.55f, 2.62f, .1f, .45f, dark); }
                    }, gy + .1f);
                    // the gate's mechanism box beside the post, toward the road
                    gear.Piece(() => { var gm = f.P(-.55f, 0f, 0f); BoxF(mb, MakeFrame(gm, new Vector2(f.F.x, f.F.z), 1f), -.25f, .25f, .9f, 1.6f, -.2f, .2f, dark); }, gy + .2f);
                    // the two lamps, each its own object (RailCrossing flashes them)
                    foreach (float u in new[] { -.55f, .55f })
                    {
                        var lm = new MeshBuilder(); var lf = MakeFrame(Vector3.zero, -come, 1f);
                        BoxF(lm, lf, -.19f, .19f, -.18f, .18f, -.01f, .01f, lampOff);
                        var lo = lm.ToObject($"Railroad crossing {gi} lamp {(s < 0f ? "a" : "b")}{(u < 0f ? 0 : 1)}", mats.House, go.transform, false, false);
                        lo.transform.position = f.P(u, 2.35f, .2f); GameObjectUtility.SetStaticEditorFlags(lo, 0); lamps.Add(lo.GetComponent<Renderer>()); gear.Face(lo);   // (hidden when the post breaks)
                    }
                    gear.End(new Vector3(p.x, gy + 1.5f, p.y), new Vector3(come.x, 0f, come.y), 1.2f, 3f, .8f);
                    // the gate arm: its hinge on the mechanism, the arm along the hinge's +X (toward the road's middle) when down, rotated 90 degrees up
                    var toCenter = new Vector3(-right.x, 0f, -right.y);
                    var hinge = new GameObject($"Railroad crossing {gi} gate {(s < 0f ? "a" : "b")}").transform; hinge.SetParent(go.transform, false);
                    hinge.position = f.P(-.55f, 1.25f, -.3f); hinge.rotation = Quaternion.LookRotation(Vector3.Cross(toCenter, Vector3.up), Vector3.up);
                    float L = half + .9f - .4f;   // to about the road's middle
                    var am = new MeshBuilder(); var af = MakeFrame(Vector3.zero, Vector2.up, 1f);
                    int bands = Mathf.Max(3, Mathf.RoundToInt(L / .9f));
                    for (int k = 0; k < bands; k++) { float x0 = .3f + (L - .3f) * k / bands, x1 = .3f + (L - .3f) * (k + 1) / bands; am.Tint = k % 2 == 0 ? red : white; Beam(am, new Vector3(x0, 0f, 0f), new Vector3(x1, 0f, 0f), .1f, .1f, k % 2 == 0 ? red : white); }
                    BoxF(am, af, -.15f, .15f, -.2f, .2f, -.9f, .2f, dark);   // the counterweight behind the hinge (along -X: the frame's u is -x)
                    var arm = am.ToObject($"Railroad crossing {gi} arm {(s < 0f ? "a" : "b")}", mats.House, hinge, false, true);
                    GameObjectUtility.SetStaticEditorFlags(arm, 0); arm.transform.localPosition = Vector3.zero; arm.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    arms.Add(arm.transform);
                }
                rc.arms = arms.ToArray(); rc.lamps = lamps.ToArray();
                GameObjectUtility.SetStaticEditorFlags(go, 0);
                gi++;
            }
            gear.ToObjects("Railroad crossing gear", mats.House, root, 1f, BreakSounds.Kind.Lamp);
            Debug.Log($"[Round78] A: {RailCrossings.Count} level crossings of a track in {groups.Count} crossings, gates and lamps for the running train");
            return gear.Count;
        }

        // the lines (rail top) and the routes the trains run (World/Rail/RailNetwork joins them: each line cut where the next one leaves it)
        static void WriteRailData()
        {
            var o = new RailOut { lines = RailPlans.Select(rp => new RailOutLine
            {
                src = rp.src, x = rp.p.Select(q => (float)Math.Round(q.x, 2)).ToArray(), z = rp.p.Select(q => (float)Math.Round(q.y, 2)).ToArray(),
                y = rp.y.Select(v => (float)Math.Round(v + RailTop, 3)).ToArray()
            }).ToArray() };
            // the main line through Marietta: from the south edge on the double track (northbound on line 0, southbound on line 6), then on to Kennesaw
            // (line 3, to the northwest edge) or to the east edge (line 2, the Georgia Northeastern); railroad.json's line numbers
            o.routes = new[]
            {
                new RailOutRoute { name = "Kennesaw north", lines = new[] { 0, 3 }, dirs = new[] { 1, 1 } },
                new RailOutRoute { name = "Kennesaw south", lines = new[] { 3, 6 }, dirs = new[] { -1, -1 } },
                new RailOutRoute { name = "Canton north", lines = new[] { 0, 2 }, dirs = new[] { 1, -1 } },
                new RailOutRoute { name = "Canton south", lines = new[] { 2, 6 }, dirs = new[] { 1, -1 } },
            };
            var full = Path.Combine(ProjectRoot, RailDataPath); Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, JsonUtility.ToJson(o));
            AssetDatabase.ImportAsset(RailDataPath, ImportAssetOptions.ForceUpdate);
        }

        // the train kit: one object per car kind, its mesh about its own middle at the rails' top (+Z forward), inactive; RailTrains copies them
        static void BuildTrainKit(Materials mats, Transform root)
        {
            var kit = new GameObject("Train kit"); kit.transform.SetParent(root, false); kit.AddComponent<TrainKit>();
            var f = MakeFrame(Vector3.zero, Vector2.up, 1f);   // u across (mirrored: symmetric shapes), v along +Z, y up
            var black = new Color(.07f, .07f, .08f); var steel = new Color(.22f, .22f, .23f); var glass = new Color(.10f, .14f, .17f); var lampC = new Color(.95f, .92f, .70f);
            void Trucks(MeshBuilder mb, float at)
            {
                foreach (float z in new[] { -at, at })
                {
                    BoxF(mb, f, -1.25f, 1.25f, .35f, .95f, z - 1.4f, z + 1.4f, steel);
                    foreach (float u in new[] { -.78f, .78f }) foreach (float dz in new[] { -.9f, .9f }) BoxF(mb, f, u - .08f, u + .08f, .02f, .9f, z + dz - .44f, z + dz + .44f, black);
                }
            }
            GameObject Make(string name, MeshBuilder mb, Vector3 size)
            {
                var go = mb.ToObject($"Train kit {name}", mats.House, kit.transform, false, true); GameObjectUtility.SetStaticEditorFlags(go, 0);
                var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, size.y * .5f + .2f, 0f); bc.size = new Vector3(size.x, size.y - .4f, size.z);
                return go;
            }
            // locomotives: a road-switcher hood unit, 21.5 m, in two made-up liveries (dark green with a yellow nose, gray with a red band)
            foreach (var (nm, body, band) in new[] { ("loco green", new Color(.12f, .26f, .19f), new Color(.92f, .74f, .12f)), ("loco gray", new Color(.40f, .42f, .44f), new Color(.70f, .13f, .10f)) })
            {
                var mb = new MeshBuilder(); Trucks(mb, 7.2f);
                BoxF(mb, f, -1.55f, 1.55f, .95f, 1.45f, -10.75f, 10.75f, black);                    // the frame
                BoxF(mb, f, -1.4f, 1.4f, .45f, 1.0f, -3.4f, 3.4f, black);                          // the fuel tank
                BoxF(mb, f, -1.2f, 1.2f, 1.45f, 4.15f, -10.2f, 4.6f, body);                       // the long hood
                BoxF(mb, f, -1.23f, 1.23f, 2.3f, 2.55f, -10.2f, 4.6f, band);                       // its band
                BoxF(mb, f, -.45f, .45f, 4.15f, 4.4f, -8f, -5f, black);                            // the fans
                BoxF(mb, f, -1.5f, 1.5f, 1.45f, 4.65f, 4.6f, 7.6f, body);                         // the cab
                BoxF(mb, f, -1.52f, 1.52f, 3.4f, 4.15f, 4.9f, 7.3f, glass);                        // its side windows
                BoxF(mb, f, -1.3f, 1.3f, 3.4f, 4.2f, 7.55f, 7.62f, glass);                         // the windshield
                BoxF(mb, f, -1.2f, 1.2f, 1.45f, 3.3f, 7.6f, 10.1f, band);                          // the short nose
                BoxF(mb, f, -.25f, .25f, 2.8f, 3.1f, 10.1f, 10.16f, lampC);                         // the headlight
                BoxF(mb, f, -.25f, .25f, 3.7f, 3.95f, -10.26f, -10.2f, lampC);                      // the rear light
                foreach (float u in new[] { -1.5f, 1.5f }) Beam(mb, f.P(u, 2.4f, -10.6f), f.P(u, 2.4f, 10.6f), .05f, .05f, band);   // the walkway rails
                Make(nm, mb, new Vector3(3.1f, 4.65f, 21.5f));
            }
            // boxcars: 16 m, in four plain colors
            foreach (var (nm, c) in new[] { ("boxcar red", new Color(.45f, .16f, .10f)), ("boxcar blue", new Color(.18f, .27f, .38f)), ("boxcar green", new Color(.22f, .33f, .22f)), ("boxcar yellow", new Color(.78f, .62f, .16f)) })
            {
                var mb = new MeshBuilder(); Trucks(mb, 5.6f);
                BoxF(mb, f, -1.45f, 1.45f, .95f, 1.25f, -8f, 8f, black);
                BoxF(mb, f, -1.5f, 1.5f, 1.25f, 4.45f, -7.8f, 7.8f, c);
                foreach (float u in new[] { -1.52f, 1.52f }) BoxF(mb, f, u - .02f, u + .02f, 1.3f, 4.2f, -1.6f, 1.6f, Shade(c, .75f));   // the sliding doors
                for (float z = -7.2f; z <= 7.3f; z += 1.2f) foreach (float u in new[] { -1.53f, 1.53f }) if (Mathf.Abs(z) > 1.8f) BoxF(mb, f, u - .02f, u + .02f, 1.3f, 4.4f, z - .05f, z + .05f, Shade(c, .85f));   // the ribs
                BoxF(mb, f, -.3f, .3f, 4.45f, 4.52f, -7.8f, 7.8f, steel);                                     // the roof walk
                Make(nm, mb, new Vector3(3.0f, 4.5f, 16f));
            }
            // tank cars: 17 m, a round barrel on a frame, black and white
            foreach (var (nm, c) in new[] { ("tank black", new Color(.09f, .09f, .10f)), ("tank white", new Color(.82f, .82f, .80f)) })
            {
                var mb = new MeshBuilder(); Trucks(mb, 6.3f);
                BoxF(mb, f, -1.2f, 1.2f, .95f, 1.25f, -8.5f, 8.5f, black);
                HBarrel(mb, new Vector3(0f, 2.75f, 0f), 1.5f, 15.6f, c);
                Cyl(mb, new Vector3(0f, 4.1f, 0f), .45f, .55f, .5f, Shade(c, .8f), 10);   // the dome
                foreach (float z in new[] { -8.3f, 8.3f }) BoxF(mb, f, -1.4f, 1.4f, 1.25f, 1.35f, z - .2f, z + .2f, steel);
                Make(nm, mb, new Vector3(3.0f, 4.6f, 17f));
            }
            // covered hoppers: 18 m, three bays sloping to the outlets, gray and tan
            foreach (var (nm, c) in new[] { ("hopper gray", new Color(.62f, .63f, .62f)), ("hopper tan", new Color(.66f, .56f, .40f)) })
            {
                var mb = new MeshBuilder(); Trucks(mb, 6.8f);
                BoxF(mb, f, -1.5f, 1.5f, 2.2f, 4.4f, -8.6f, 8.6f, c);
                for (int k = 0; k < 3; k++)
                {
                    float z = -5.3f + k * 5.3f; mb.Tint = Shade(c, .85f);
                    Face(mb, f.P(-1.5f, 2.2f, z - 2.5f), f.P(1.5f, 2.2f, z - 2.5f), f.P(.5f, 1.0f, z - .5f), f.P(-.5f, 1.0f, z - .5f), new Vector3(0f, -.6f, -1f));
                    Face(mb, f.P(-.5f, 1.0f, z + .5f), f.P(.5f, 1.0f, z + .5f), f.P(1.5f, 2.2f, z + 2.5f), f.P(-1.5f, 2.2f, z + 2.5f), new Vector3(0f, -.6f, 1f));
                    BoxF(mb, f, -.5f, .5f, .8f, 1.0f, z - .5f, z + .5f, black);
                }
                BoxF(mb, f, -.25f, .25f, 4.4f, 4.5f, -8.4f, 8.4f, steel);
                BoxF(mb, f, -1.2f, 1.2f, .95f, 1.3f, -9f, -7.6f, black); BoxF(mb, f, -1.2f, 1.2f, .95f, 1.3f, 7.6f, 9f, black);
                Make(nm, mb, new Vector3(3.0f, 4.5f, 18f));
            }
            kit.SetActive(false);
        }

        // a closed barrel along Z: 14 sides and flat ends
        static void HBarrel(MeshBuilder mb, Vector3 c, float r, float len, Color col)
        {
            mb.Tint = col; int seg = 14, s0 = mb.Count;
            for (int k = 0; k < seg; k++) { float a = Mathf.PI * 2f * k / seg; var o = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f); mb.Vertex(c + o + Vector3.back * len * .5f); mb.Vertex(c + o + Vector3.forward * len * .5f); }
            for (int k = 0; k < seg; k++) { int i0 = s0 + k * 2, i1 = s0 + ((k + 1) % seg) * 2; mb.Tri(i0, i1, i0 + 1); mb.Tri(i1, i1 + 1, i0 + 1); }
            int cb = mb.Vertex(c + Vector3.back * len * .5f), cf = mb.Vertex(c + Vector3.forward * len * .5f);
            for (int k = 0; k < seg; k++) { int i0 = s0 + k * 2, i1 = s0 + ((k + 1) % seg) * 2; mb.Tri(cb, i1, i0); mb.Tri(cf, i0 + 1, i1 + 1); }
        }
    }
}
