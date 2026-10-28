using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 80 (D1, Round 79's found-not-changed item: "Express Lanes 13's 780 m deck flies 2.06 s and lands hard (10.7 m/s) driven against its direction:
// with E the lanes run both ways, so smooth it both ways (and every Express Lanes deck: drive each both ways, list air and landings)"): Express Lanes 13 is
// the I-75 lanes' long deck (three OSM pieces end to end, 1833 m, (2049, -4662) to (3269, -6027)). Round 27's SmoothBridgeEnds laid every chain of pieces
// straight between its two ends' road heights: there that straight deck ran 10 m under the Canton Road Connector and two ramps where they cross it (banks
// across the lanes; driven against their way the car met one and flew). That chain alone (the others are listed for Dan, not changed): its own profile
// smoothed (Round 77's membrane, its two outer ends held) until no crest is sharper than a vertical curve a car at 40 m/s stays down on (radius
// ExpressCrestR: a second difference of h^2 / R at the deck's spacing h) and no sag sharper than twice that; where a road, a deck or the railroad crosses
// under it, never lowered (each such point held at its height, or 0.25 m over the ground there if that is higher, a cap of the same curve falling away from
// it); SmoothBridgeEnds then keeps that shape (its ends still eased onto the road: ExpressKept). Runs in the planning after RailDecks and before the bridge
// ends are graded (anything that changes a deck's heights must: Round 79). Report Assets/Generated/round80_express_decks.txt.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const string ExpressName = "Northwest Corridor Express Lanes";
        const float ExpressCrestR = 160f;

        // the deck's points with something crossing under them: a road, another road's deck, a rail line (its segment crossing the deck's line), the points
        // within the crossing's half width and 3 m of it along the deck
        static List<int> HeldUnder(MapData map, List<Bridge> own, List<Vector2> pts, float[] cum)
        {
            var held = new HashSet<int>();
            float x0 = pts.Min(q => q.x) - 20f, x1 = pts.Max(q => q.x) + 20f, z0 = pts.Min(q => q.y) - 20f, z1 = pts.Max(q => q.y) + 20f;
            var lines = new List<(float[] p, float half)>();
            if (map.roads != null) foreach (var r in map.roads) if (r.points != null && r.name != ExpressName) lines.Add((r.points, r.width * .5f));
            if (map.bridges != null) foreach (var b in map.bridges) if (b.points != null && !own.Contains(b) && b.name != ExpressName) lines.Add((b.points, b.width * .5f));
            var rp = System.IO.Path.Combine(ProjectRoot, "Data/tracks/railroad.json");
            if (System.IO.File.Exists(rp)) { var rj = JsonUtility.FromJson<RailJ>(System.IO.File.ReadAllText(rp)); if (rj?.lines != null) foreach (var l in rj.lines) if (l.points != null) lines.Add((l.points, 3f)); }
            foreach (var (lp, half) in lines)
                for (int k = 0; k + 3 < lp.Length; k += 2)
                {
                    var a = new Vector2(lp[k], lp[k + 1]); var b = new Vector2(lp[k + 2], lp[k + 3]);
                    if (Mathf.Max(a.x, b.x) < x0 || Mathf.Min(a.x, b.x) > x1 || Mathf.Max(a.y, b.y) < z0 || Mathf.Min(a.y, b.y) > z1) continue;
                    for (int i = 0; i + 1 < pts.Count; i++)
                    {
                        if (!SegCross(a, b, pts[i], pts[i + 1], out float t)) continue;
                        float sAt = Mathf.Lerp(cum[i], cum[i + 1], t), reach = half + 3f;
                        for (int j = 0; j < pts.Count; j++) if (Mathf.Abs(cum[j] - sAt) <= reach) held.Add(j);
                    }
                }
            return held.OrderBy(i => i).ToList();
        }
        static bool SegCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out float tOnCd)
        {
            tOnCd = 0f; var r = b - a; var q = d - c; float den = r.x * q.y - r.y * q.x; if (Mathf.Abs(den) < 1e-6f) return false;
            var ac = c - a; float ta = (ac.x * q.y - ac.y * q.x) / den, tc = (ac.x * r.y - ac.y * r.x) / den;
            tOnCd = tc; return ta >= 0f && ta <= 1f && tc >= 0f && tc <= 1f;
        }

        // the decks SmoothBridgeEnds keeps the shape of (Express Lanes 13's chain)
        static readonly HashSet<Bridge> ExpressKept = new HashSet<Bridge>();
        static void ExpressDecks(MapData map, TerrainGrid terrain)
        {
            ExpressKept.Clear();
            if (map.bridges == null) return;
            var log = new List<string>();
            var pieces = map.bridges.Where(b => b.name == ExpressName && b.points != null && b.deck_y != null && b.deck_y.Length >= 2 && b.points.Length / 2 == b.deck_y.Length).ToList();
            // chains: pieces whose ends meet (within 1.5 m), in order
            var left = new List<Bridge>(pieces); var chains = new List<List<(Bridge b, bool rev)>>();
            Vector2 First(Bridge b, bool rev) { var p = ToPoints(b.points); return rev ? p[p.Count - 1] : p[0]; }
            Vector2 Last(Bridge b, bool rev) { var p = ToPoints(b.points); return rev ? p[0] : p[p.Count - 1]; }
            while (left.Count > 0)
            {
                var chain = new List<(Bridge b, bool rev)> { (left[0], false) }; left.RemoveAt(0);
                for (bool grew = true; grew;)
                {
                    grew = false;
                    for (int i = 0; i < left.Count; i++)
                    {
                        var b = left[i]; var end = Last(chain[chain.Count - 1].b, chain[chain.Count - 1].rev); var start = First(chain[0].b, chain[0].rev);
                        if ((First(b, false) - end).magnitude < 1.5f) { chain.Add((b, false)); }
                        else if ((Last(b, false) - end).magnitude < 1.5f) { chain.Add((b, true)); }
                        else if ((Last(b, false) - start).magnitude < 1.5f) { chain.Insert(0, (b, false)); }
                        else if ((First(b, false) - start).magnitude < 1.5f) { chain.Insert(0, (b, true)); }
                        else continue;
                        left.RemoveAt(i); grew = true; break;
                    }
                }
                chains.Add(chain);
            }
            foreach (var chain in chains)
            {
                // the chain's profile (a shared end point once)
                var pts = new List<Vector2>(); var ys = new List<float>(); var owner = new List<(Bridge b, int i)>();
                foreach (var (b, rev) in chain)
                {
                    var p = ToPoints(b.points); int n = p.Count;
                    for (int k = 0; k < n; k++)
                    {
                        int i = rev ? n - 1 - k : k;
                        if (k == 0 && pts.Count > 0) { owner.Add((b, i)); continue; }   // the joint: the earlier piece's point stands for both
                        pts.Add(p[i]); ys.Add(b.deck_y[i]); owner.Add((b, i));
                    }
                }
                int m = ys.Count; if (m < 3) continue;
                // Express Lanes 13's chain only (its longest piece over 700 m): the others as they were
                if (!chain.Any(x => { var q = ToPoints(x.b.points); float l = 0f; for (int i = 1; i < q.Count; i++) l += (q[i] - q[i - 1]).magnitude; return l > 700f; })) continue;
                // a chain over the old core's edge is Round 62's (R62SeamOverpasses lays it with its roads as one curve, after this)
                if (pts.Any(q => Mathf.Abs(q.y) < .3f && q.x >= 0f && q.x <= map.size_x_m || Mathf.Abs(q.x) < .3f && q.y >= 0f && q.y <= map.size_z_m)) { log.Add($"{ExpressName} at ({pts[0].x:0}, {pts[0].y:0}): over the old core's edge, Round 62's (left)"); continue; }
                float len = 0f; for (int i = 1; i < m; i++) len += (pts[i] - pts[i - 1]).magnitude;
                float h = len / (m - 1), c = h * h / ExpressCrestR;
                float Crest(float[] a) { float w = 0f; for (int i = 1; i + 1 < a.Length; i++) w = Mathf.Max(w, -(a[i + 1] - 2f * a[i] + a[i - 1])); return w; }
                float Sag(float[] a) { float w = 0f; for (int i = 1; i + 1 < a.Length; i++) w = Mathf.Max(w, a[i + 1] - 2f * a[i] + a[i - 1]); return w; }
                var raw = ys.ToArray(); float c0 = Crest(raw), s0 = Sag(raw);
                string name = $"{ExpressName} ({pts[0].x:0}, {pts[0].y:0} to {pts[m - 1].x:0}, {pts[m - 1].y:0}, {len:0} m, {chain.Count} piece(s))";
                if (c0 <= c && s0 <= 2f * c) { log.Add($"{name}: smooth already (crest {c0:0.000} m, sag {s0:0.000} m a step; limit {c:0.000})"); continue; }
                var wt = new float[m]; for (int i = 0; i < m; i++) wt[i] = i == 0 || i == m - 1 ? 1e6f : 1f;
                float reach = 6f; float[] sm = raw;
                for (int tries = 0; tries < 9; tries++) { sm = R77Smooth1D(raw, wt, (reach / h) * (reach / h)); if (Crest(sm) <= c && Sag(sm) <= 2f * c) break; reach *= 1.6f; }
                // where a road, another deck or the railroad crosses under it the deck is never lowered: each such point holds its height and a cap of
                // the same curve (ExpressCrestR) falls away from it; the deck is the higher of the smoothed profile and the caps (where they meet, a sag)
                var cum = new float[m]; for (int i = 1; i < m; i++) cum[i] = cum[i - 1] + (pts[i] - pts[i - 1]).magnitude;
                var held = HeldUnder(map, chain.Select(x => x.b).ToList(), pts, cum);
                if (held.Count > 0)
                    for (int i = 0; i < m; i++)
                        foreach (int j in held) { float dd = cum[i] - cum[j]; sm[i] = Mathf.Max(sm[i], Mathf.Max(raw[j], terrain.Height(pts[j].x, pts[j].y) + .25f) - dd * dd / (2f * ExpressCrestR)); }
                for (int i = 0; i < m; i++) if (i == 0 || i == m - 1) sm[i] = raw[i];
                float up = 0f, down = 0f; for (int i = 0; i < m; i++) { up = Mathf.Max(up, sm[i] - raw[i]); down = Mathf.Max(down, raw[i] - sm[i]); }
                float downHeld = 0f; foreach (int j in held) downHeld = Mathf.Max(downHeld, raw[j] - sm[j]);
                // back into the pieces
                int at = 0;
                foreach (var (b, rev) in chain)
                {
                    int n = b.deck_y.Length;
                    for (int k = 0; k < n; k++)
                    {
                        int i = rev ? n - 1 - k : k;
                        int idx = k == 0 && at > 0 ? at - 1 : at++;
                        b.deck_y[i] = sm[idx];
                    }
                    ExpressKept.Add(b);
                }
                log.Add($"{name}: crest {c0:0.000} -> {Crest(sm):0.000} m a step, sag {s0:0.000} -> {Sag(sm):0.000} m (limit {c:0.000} / {2f * c:0.000} at {h:0.0} m spacing, a {ExpressCrestR:0} m curve) " +
                        $"smoothed over {reach:0} m; {held.Count} point(s) over something held (lowered there at most {downHeld:0.00} m); raised at most {up:0.00} m, lowered at most {down:0.00} m; the outer ends kept");
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(ProjectRoot, GeneratedDir, "round80_express_decks.txt"), string.Join("\n", log) + "\n");
            Debug.Log($"[Round80] D1: {chains.Count} Express Lanes deck chain(s) of {pieces.Count} piece(s): " + string.Join("; ", log));
        }

        // ------------------------------------------------------------ E: the Express Lanes' gates
        // Round 80 (E, Dan's photo: "a red and white striped swing arm with red lamps along its top, a red DO NOT ENTER banner hung under it, the green
        // EXPRESS LANE sign and the keep-left / keep-right sign by it"): a gate on every ramp on or off the lanes (an unnamed motorway_link with an end on
        // them; a link that leads from the lanes back onto them, I-575's connector to I-75's, is part of them and has none), 30 m along it from the lanes,
        // its post on the right of a driver coming up the ramp toward the lanes, its arm across the ramp (built swung shut; ExpressGate swings it), its
        // banner and signs facing that driver. Game-independent world piece (ExpressLanes runs the schedule).
        static void BuildExpressGates(MapData map, TerrainGrid terrain, Materials mats, Transform world)
        {
            var exPts = new List<Vector2>();
            foreach (var r in map.roads) if (r.name == ExpressName && r.points != null) exPts.AddRange(ToPoints(r.points));
            if (map.bridges != null) foreach (var b in map.bridges) if (b.name == ExpressName && b.points != null) exPts.AddRange(ToPoints(b.points));
            if (exPts.Count == 0) return;
            bool OnLanes(Vector2 q) => exPts.Any(e => (e - q).sqrMagnitude < 4f);
            var links = new List<(List<Vector2> p, bool oneway, float width, float[] deck, long id)>();
            foreach (var r in map.roads) if (r.@class == "motorway_link" && string.IsNullOrEmpty(r.name) && r.points != null) links.Add((ToPoints(r.points), r.oneway, r.width, null, r.osm_id));
            if (map.bridges != null) foreach (var b in map.bridges) if (b.@class == "motorway_link" && string.IsNullOrEmpty(b.name) && b.points != null) links.Add((ToPoints(b.points), b.oneway, b.width, b.deck_y, -1));
            // a link that reaches the lanes again within a few pieces (the connector): no gate
            bool BackOnLanes(int li, Vector2 from)
            {
                var seen = new HashSet<int> { li }; var ends = new List<Vector2>();
                var l0 = links[li].p; ends.Add((l0[0] - from).sqrMagnitude < 4f ? l0[l0.Count - 1] : l0[0]);
                for (int step = 0; step < 6 && ends.Count > 0; step++)
                {
                    var next = new List<Vector2>();
                    foreach (var end in ends)
                    {
                        if (OnLanes(end)) return true;
                        for (int k = 0; k < links.Count; k++)
                        {
                            if (seen.Contains(k)) continue; var p = links[k].p;
                            if ((p[0] - end).sqrMagnitude < 4f) { seen.Add(k); next.Add(p[p.Count - 1]); }
                            else if ((p[p.Count - 1] - end).sqrMagnitude < 4f) { seen.Add(k); next.Add(p[0]); }
                        }
                    }
                    ends = next;
                }
                return false;
            }
            var root = new GameObject("Express Lanes"); root.transform.SetParent(world, false);
            var lanes = root.AddComponent<ExpressLanes>(); var gates = new List<ExpressGate>(); var log = new List<string>();
            var lane = RoadSignMaterial("ExpressLane"); var keep = RoadSignMaterial("ExpressKeepRight"); var banner = RoadSignMaterial("ExpressDoNotEnter");
            var south2 = ExpressLanes.South;
            for (int li = 0; li < links.Count; li++)
            {
                var (p, oneway, width, deck, id) = links[li];
                if (p.Count < 2) continue;
                bool atStart = OnLanes(p[0]), atEnd = OnLanes(p[p.Count - 1]);
                if (atStart == atEnd) continue;   // not a ramp on or off them
                var J = atStart ? p[0] : p[p.Count - 1];
                if (BackOnLanes(li, J)) { log.Add($"link {id} at ({J.x:0}, {J.y:0}): from the lanes back onto them (no gate)"); continue; }
                // along the link from the lanes: the point 30 m on (its middle when shorter) and the way out there
                var q = atStart ? p : Enumerable.Reverse(p).ToList(); float len = 0f; for (int i = 1; i < q.Count; i++) len += (q[i] - q[i - 1]).magnitude;
                float at = Mathf.Min(30f, len * .5f), acc = 0f; Vector2 G = q[0], f = (q[1] - q[0]).normalized; int seg = 0;
                for (int i = 1; i < q.Count; i++) { float l = (q[i] - q[i - 1]).magnitude; if (acc + l >= at) { G = Vector2.Lerp(q[i - 1], q[i], (at - acc) / Mathf.Max(l, 1e-4f)); f = (q[i] - q[i - 1]).normalized; seg = i; break; } acc += l; }
                float y = Ground(terrain, G.x, G.y);
                if (deck != null && deck.Length == p.Count) { int di = atStart ? seg : p.Count - 1 - seg; y = deck[Mathf.Clamp(di, 0, deck.Length - 1)]; }
                // which way it is used: a two-way link both ways; a one-way link the way its traffic heads at the lanes
                var outward = (q[1] - q[0]).normalized;
                bool towardLanes = !atStart;   // its points run toward the lanes: traffic on it goes onto them
                var heading = towardLanes ? -outward : outward;
                bool south = Vector2.Dot(heading, south2) > 0f;
                bool usableSouth = !oneway || south, usableNorth = !oneway || !south;
                // the gate's frame at its post: local +Z out along the ramp (away from the lanes), local +X across it
                var f3 = new Vector3(f.x, 0f, f.y); var across = Vector3.Cross(Vector3.up, f3).normalized;
                float W = Mathf.Max(4.5f, width) + 1.2f;
                var postAt = new Vector3(G.x, y, G.y) - across * (W * .5f + .5f);
                int no = gates.Count + 1;
                var gateGo = new GameObject($"Express gate {no}"); gateGo.transform.SetParent(root.transform, false);
                gateGo.transform.SetPositionAndRotation(postAt, Quaternion.LookRotation(f3, Vector3.up));
                var gate = gateGo.AddComponent<ExpressGate>(); gate.usableSouth = usableSouth; gate.usableNorth = usableNorth;
                // the post and the motor box (still), the signs' post 4 m out on the same side: green EXPRESS LANE over the keep-right sign
                var still = new MeshBuilder();
                Post(still, new Vector3(-.35f, -.3f, -.35f), 1.55f, .7f, new Color(.55f, .56f, .55f));
                var sp = new Vector3(-.6f, -.3f, 4f);
                Post(still, sp, 4.3f, .12f, new Color(.6f, .6f, .6f));
                still.ToObject($"Express gate {no} post", mats.House, gateGo.transform, false);
                Plate(gateGo.transform, sp + new Vector3(0f, 3.4f, .08f), 1.0f, 1.5f, lane, $"ExpressLane_{no}");
                Plate(gateGo.transform, sp + new Vector3(0f, 1.95f, .08f), .8f, 1.2f, keep, $"ExpressKeep_{no}");
                // the hinge and the arm (turning)
                var hinge = new GameObject("Hinge").transform; hinge.SetParent(gateGo.transform, false);
                var armGo = new GameObject("Arm"); armGo.transform.SetParent(hinge, false);
                var armMb = new MeshBuilder(); float L = W + .4f;
                int n = Mathf.Max(4, Mathf.RoundToInt(L / .75f));
                for (int k = 0; k < n; k++) Beam(armMb, new Vector3(.3f + L * k / n, 1.05f, 0f), new Vector3(.3f + L * (k + 1) / n, 1.05f, 0f), .1f, .12f, k % 2 == 0 ? new Color(.9f, .9f, .88f) : new Color(.82f, .08f, .08f), Vector3.forward);
                for (float x = .9f; x < L; x += 1.4f) Post(armMb, new Vector3(.3f + x, 1.11f, 0f), .1f, .09f, new Color(1f, .15f, .1f));   // the red lamps on top
                float bw = Mathf.Min(3.6f, L * .7f), bh = bw * .25f, bx = .3f + L * .5f;
                armMb.Tint = new Color(.75f, .1f, .12f);   // the banner's back, plain red
                Face(armMb, new Vector3(bx - bw / 2, .99f - bh, -.02f), new Vector3(bx + bw / 2, .99f - bh, -.02f), new Vector3(bx + bw / 2, .99f, -.02f), new Vector3(bx - bw / 2, .99f, -.02f), Vector3.back);
                armMb.ToObject($"Express gate {no} arm", mats.House, armGo.transform, false);
                Plate(armGo.transform, new Vector3(bx, .99f - bh / 2f, 0f), bw, bh, banner, $"ExpressBanner_{no}");
                var kin = armGo.AddComponent<Rigidbody>(); kin.isKinematic = true; kin.useGravity = false; armGo.AddComponent<ExpressGateArm>().gate = gate;   // (it turns: a kinematic body; its trigger tells the gate)
                var trig = armGo.AddComponent<BoxCollider>(); trig.isTrigger = true; trig.center = new Vector3(.3f + L * .5f, .8f, 0f); trig.size = new Vector3(L, 1.6f, .6f);
                foreach (var t in gateGo.GetComponentsInChildren<Transform>(true)) if (t.IsChildOf(hinge)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                GameObjectUtility.SetStaticEditorFlags(gateGo, 0);
                gate.hinge = hinge; gate.arm = armGo; gate.openYaw = -88f;
                gates.Add(gate);
                log.Add($"gate {no}: link {(id > 0 ? id.ToString() : "(a deck)")} at ({G.x:0.0}, {G.y:0.0}) {y:0.0} m, {(oneway ? (towardLanes ? "one-way onto the lanes" : "one-way off the lanes") : "two-way")}, used " +
                        (usableSouth && usableNorth ? "both ways" : usableSouth ? "southbound" : "northbound"));
            }
            lanes.gates = gates.ToArray();
            System.IO.File.AppendAllText(System.IO.Path.Combine(ProjectRoot, GeneratedDir, "round80_express_decks.txt"), "\nGates:\n" + string.Join("\n", log) + "\n");
            Debug.Log($"[Round80] E: {gates.Count} Express Lanes gates: " + string.Join("; ", log));
        }
        // a one-sided sign face: a textured quad facing local +Z of its parent (the drivers it is for), a child of 'parent'
        static void Plate(Transform parent, Vector3 local, float w, float h, Material mat, string name)
        {
            if (mat == null) return;
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = local; go.transform.localRotation = Quaternion.identity;
            var mesh = new Mesh { name = name }; float x = w * .5f, y = h * .5f;
            mesh.vertices = new[] { new Vector3(x, -y, 0), new Vector3(-x, -y, 0), new Vector3(-x, y, 0), new Vector3(x, y, 0) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/{name}.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
