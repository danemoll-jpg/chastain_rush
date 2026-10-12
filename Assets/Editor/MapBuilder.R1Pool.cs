using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

// Round 20 follow-up (Dan, 2026-10-12: "I don't mind this area as a shortcut, but it has to has some risk"): R1 on Dan's own line
// across the west lobe, reverse only (Tools/mapgen/make_shortcuts.py, build_r1, says where and why). Built here from the shortcut
// file's 'pool', 'fences', 'props' and 'tracks':
//   grade   the side yard between 1987 and 1552 levelled (a lawn behind the ramp's fence; 2.5 m of bank round it back to the
//           ground, never inside a house, never below a house's foundation), and a level pad under the pool
//   pool    (Dan, 03:51: "I would prefer it was an above ground pool ... you would drop out of it") a round above-ground pool in the
//           back yards behind 1572 / 1622: a steel wall with a top rail and uprights, a blue liner, the water (ShallowWater: a
//           vehicle in it slows right down), an A-frame ladder on the near side. Its wall is solid (BoxColliders round it); its
//           floor is the levelled ground. The way out is the shortcut's two prisms (MapBuilder.Shortcuts.cs): a wooden ramp inside,
//           from the floor up to the rim on the gate's side, and a small deck on the rim beyond it, whose outer edge you drop off
//   fences  solid privacy fences (the neighborhood's panels, a real collider per panel, nothing breaks) across the lobe's side
//           yards; the one across 1572 / 1622 has an open side gate (round solid posts, its two leaves swung back flat); the
//           ones in #6's corridor stand only during a reverse race (RaceOnly)
//   props   patio chairs, a table, an umbrella and a grill, breakable (FenceChunk, as the mailboxes)
//   tracks  worn tire tracks from the lap to the ramp's foot (the hint, with the ramp itself in plain sight)
// The ramp is a prism (MapBuilder.Shortcuts.cs) with the kids' ramp's homemade look (MapBuilder.PartyHint.cs); its lip speed
// cap is the shortcut's big_jump (BigJump).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class ScPool { public float[] yard, c, axis, front; public float yard_y, r, pad_y, wall_h, water_h, clean_from, pool_speed; }
        [Serializable] class ScGate { public float[] c; public float w; }
        [Serializable] class ScFence { public float[] a, b; public string label, kind; public ScGate gate; }
        [Serializable] class ScProp { public string kind; public float x, z, yaw; }

        static bool HasPool(ScPool pl) => pl != null && pl.c != null && pl.c.Length >= 2 && pl.r > 0f;

        sealed partial class Neighborhood
        {
            // the lots' hashed fences (BuildFences) are not kept off a shortcut's line; on R1 one ran down the middle of its first
            // side yard: none in the levelled yard, round the pool, or within 2.5 m of its line off the street
            public bool OnPoolJumpYard(Vector2 p)
            {
                if (ShortcutData == null) return false;
                foreach (var sc in ShortcutData.shortcuts)
                {
                    if (sc.look != "pool_jump") continue;
                    if (sc.pool != null && sc.pool.yard != null && sc.pool.yard.Length >= 8) { var yd = ToPoints(sc.pool.yard); if (InPolygon(p, yd) || DistToEdges(p, yd) < 1f) return true; }
                    if (HasPool(sc.pool) && (p - new Vector2(sc.pool.c[0], sc.pool.c[1])).magnitude < sc.pool.r + 4f) return true;
                    var line = ToPoints(sc.path);
                    for (int i = 0; i + 1 < line.Count; i++)
                    {
                        var ab = line[i + 1] - line[i]; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - line[i], ab) / ab.sqrMagnitude);
                        if ((line[i] + ab * t - p).sqrMagnitude < 2.5f * 2.5f && RoadClearance(p) > 0.5f) return true;
                    }
                }
                return false;
            }

            // the yard levelled and the pool's pad, before anything samples the ground (RegradeAndReport)
            void GradePools()
            {
                if (ShortcutData == null) return;
                foreach (var sc in ShortcutData.shortcuts)
                {
                    var pl = sc.pool; if (pl == null || pl.yard == null || pl.yard.Length < 8) continue;
                    var yard = ToPoints(pl.yard);
                    const float Bank = 2.5f;
                    int levelled = 0, held = 0; float cut = 0f, fill = 0f;
                    EditTerrain(terrain, BoundsOf(yard, Bank + 1f), (x, z, y) =>
                    {
                        var p = new Vector2(x, z);
                        if (InsideHouse(p, 0.1f, null)) return y;
                        float d = InPolygon(p, yard) ? 0f : DistToEdges(p, yard);
                        if (d > Bank) return y;
                        float ny = Mathf.Lerp(pl.yard_y - 0.02f, y, Smooth01(d / Bank));
                        // grounding (as GradeShortcuts): within 1.5 m of a house never below its foundation (no gap under a wall)
                        foreach (var h in Houses)
                        {
                            var dh = p - h.footprintCentre; if (dh.sqrMagnitude > 400f) continue;
                            if (Mathf.Abs(Vector2.Dot(dh, new Vector2(h.frame.R.x, h.frame.R.z))) < h.W / 2 + 1.5f && Mathf.Abs(Vector2.Dot(dh, new Vector2(h.frame.F.x, h.frame.F.z))) < h.D / 2 + 1.5f && ny < h.baseY + 0.15f)
                            { ny = Mathf.Min(y, h.baseY + 0.15f); held++; }
                        }
                        if (Mathf.Abs(ny - y) > 1e-3f) { levelled++; cut = Mathf.Max(cut, y - ny); fill = Mathf.Max(fill, ny - y); }
                        return ny;
                    });
                    int pad = 0;
                    if (HasPool(pl))
                    {
                        var c = new Vector2(pl.c[0], pl.c[1]); float R = pl.r + 0.6f;
                        EditTerrain(terrain, new Rect(c.x - R - 2f, c.y - R - 2f, 2f * R + 4f, 2f * R + 4f), (x, z, y) =>
                        {
                            float d = (new Vector2(x, z) - c).magnitude; if (d > R + 1.5f) return y;
                            pad++; return Mathf.Lerp(pl.pad_y - 0.02f, y, Smooth01(Mathf.Max(0f, d - R) / 1.5f));
                        });
                    }
                    Debug.Log($"[Round20+] {sc.id}: the side yard levelled at {pl.yard_y:0.00} m ({levelled} terrain samples, cut up to {cut:0.00} m, filled up to {fill:0.00} m; {held} held up at a house's foundation); the pool's pad at {pl.pad_y:0.00} m ({pad} samples)");
                }
            }
        }

        static readonly Color PoolSteel = new Color(0.78f, 0.80f, 0.82f), PoolLiner = new Color(0.30f, 0.58f, 0.86f), PoolRail = new Color(0.92f, 0.93f, 0.94f), PoolWater = new Color(0.16f, 0.70f, 0.82f);

        static void BuildPoolJump(ScShortcut sc, TerrainGrid terrain, Materials mats, Transform parent)
        {
            var pl = sc.pool;
            if (HasPool(pl))
            {
                var c = new Vector2(pl.c[0], pl.c[1]); var U = new Vector2(pl.axis[0], pl.axis[1]).normalized; float r = pl.r, y0 = pl.pad_y, rim = y0 + pl.wall_h, surf = y0 + pl.water_h;
                Vector3 At(float a, float rr, float y) => new Vector3(c.x + Mathf.Cos(a) * rr, y, c.y + Mathf.Sin(a) * rr);
                var mb = new MeshBuilder(); const int Seg = 32;
                var wallRoot = new GameObject(sc.id + " pool wall").transform; wallRoot.SetParent(parent, false);
                for (int i = 0; i < Seg; i++)
                {
                    float a0 = Mathf.PI * 2f * i / Seg, a1 = Mathf.PI * 2f * (i + 1) / Seg, am = (a0 + a1) * 0.5f;
                    var outN = new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am));
                    mb.Tint = PoolSteel; Face(mb, At(a0, r, y0 - 0.1f), At(a1, r, y0 - 0.1f), At(a1, r, rim), At(a0, r, rim), outN);    // the steel outside
                    mb.Tint = PoolLiner; Face(mb, At(a0, r - 0.05f, y0 - 0.1f), At(a1, r - 0.05f, y0 - 0.1f), At(a1, r - 0.05f, rim), At(a0, r - 0.05f, rim), -outN);   // the liner inside
                    mb.Tint = PoolRail; Face(mb, At(a0, r - 0.1f, rim + 0.04f), At(a1, r - 0.1f, rim + 0.04f), At(a1, r + 0.1f, rim + 0.04f), At(a0, r + 0.1f, rim + 0.04f), Vector3.up);   // the top rail
                    if (i % 4 == 0) Beam(mb, At(a0, r + 0.06f, y0 - 0.05f), At(a0, r + 0.06f, rim + 0.04f), 0.1f, 0.05f, PoolRail);   // an upright
                    mb.Tint = PoolLiner; TriFace(mb, new Vector3(c.x, y0 + 0.02f, c.y), At(a0, r - 0.05f, y0 + 0.02f), At(a1, r - 0.05f, y0 + 0.02f), Vector3.up);   // the floor
                    mb.Tint = PoolWater; TriFace(mb, new Vector3(c.x, surf, c.y), At(a0, r - 0.05f, surf), At(a1, r - 0.05f, surf), Vector3.up);   // the water
                    // the solid wall: a box per segment, as high as the wall
                    var seg = new GameObject("wall"); seg.transform.SetParent(wallRoot, false);
                    seg.transform.SetPositionAndRotation(At(am, r, y0 + pl.wall_h * 0.5f - 0.05f), Quaternion.LookRotation(new Vector3(-Mathf.Sin(am), 0f, Mathf.Cos(am)), Vector3.up));
                    var bc = seg.AddComponent<BoxCollider>(); bc.size = new Vector3(0.12f, pl.wall_h + 0.1f, 2f * Mathf.PI * r / Seg + 0.05f);
                }
                // the A-frame ladder on the near side (look only)
                var nearA = Mathf.Atan2(-U.y, -U.x); var n = new Vector3(-U.y, 0f, U.x);
                foreach (float side in new[] { -1f, 1f })
                {
                    var top = At(nearA, r, rim + 0.15f) + n * (side * 0.3f);
                    var outFoot = At(nearA, r + 1.0f, y0) + n * (side * 0.3f); var inFoot = At(nearA, r - 1.0f, y0) + n * (side * 0.3f);
                    Beam(mb, outFoot, top, 0.05f, 0.05f, PoolRail); Beam(mb, inFoot, top, 0.05f, 0.05f, PoolRail);
                }
                for (int k = 1; k <= 3; k++)
                {
                    float t = k / 4f; var a = Vector3.Lerp(At(nearA, r + 1.0f, y0), At(nearA, r, rim + 0.15f), t);
                    Beam(mb, a - n * 0.3f, a + n * 0.3f, 0.12f, 0.03f, PoolRail);
                }
                mb.ToObject(sc.id + " above-ground pool", mats.House, parent, false, true);
                var w = new GameObject(sc.id + " pool water").AddComponent<ShallowWater>(); w.transform.SetParent(parent, false);
                var ring = new Vector2[Seg]; for (int i = 0; i < Seg; i++) { float a = Mathf.PI * 2f * i / Seg; ring[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (r - 0.05f); }
                w.outline = ring; w.surfaceY = surf;
                var hold = w.gameObject.AddComponent<PoolHold>(); hold.centre = c; hold.radius = r - 0.1f; hold.rimY = rim;   // a fall in costs real time
                Debug.Log($"[Round20+] {sc.id} above-ground pool: {2f * r:0.0} m across at ({c.x:0.0}, {c.y:0.0}), its wall {pl.wall_h:0.00} m (solid), water {pl.water_h:0.0} m deep at {surf:0.00} m; out by the ramp inside and the deck on the gate's side");
            }
            BuildSolidFences(sc, terrain, mats, parent);
            BuildPatio(sc, terrain, mats, parent);
            if (sc.tracks != null && sc.tracks.Length >= 6) WornRuts(terrain, mats, parent, ToPoints(sc.tracks), sc.id + " worn tracks");
        }

        // solid privacy fences, the neighborhood's look (BuildFencePanel), a BoxCollider per panel; a gate: an opening between two
        // round solid posts, its two leaves swung back flat against the fence (look only)
        static void BuildSolidFences(ScShortcut sc, TerrainGrid terrain, Materials mats, Transform parent)
        {
            if (sc.fences == null || sc.fences.Length == 0) return;
            var solid = new GameObject(sc.id + " fences").transform; solid.SetParent(parent, false);
            var race = new GameObject(sc.id + " fences (reverse races only)").transform; race.SetParent(parent, false);
            race.gameObject.AddComponent<RaceOnly>().reverse = true;
            int panels = 0, n = 0;
            foreach (var fe in sc.fences)
            {
                if (fe.a == null || fe.b == null) continue;
                bool raceOnly = fe.kind == "reverse_only";
                var holder = new GameObject("Fence " + fe.label.Replace('/', '-')).transform; holder.SetParent(raceOnly ? race : solid, false);
                var mb = new MeshBuilder(); var dummy = new List<FenceChunk.Piece>();
                Color wood = Shade(PickOf(WoodTones, 4000 + n, 150), 1f);
                Vector2 A = new Vector2(fe.a[0], fe.a[1]), B = new Vector2(fe.b[0], fe.b[1]);
                var spans = new List<(Vector2 a, Vector2 b)>();
                bool gate = fe.gate != null && fe.gate.c != null && fe.gate.c.Length >= 2 && fe.gate.w > 0f;
                var dir = (B - A).normalized;
                if (gate)
                {
                    var C = new Vector2(fe.gate.c[0], fe.gate.c[1]); float t = Vector2.Dot(C - A, dir), h = fe.gate.w * 0.5f;
                    spans.Add((A, A + dir * (t - h))); spans.Add((A + dir * (t + h), B));
                }
                else spans.Add((A, B));
                foreach (var (a, b) in spans)
                {
                    float len = Vector2.Distance(a, b); int cnt = Mathf.Max(1, Mathf.RoundToInt(len / 2.4f));
                    for (int i = 0; i < cnt; i++)
                    {
                        var p = Vector2.Lerp(a, b, i / (float)cnt); var q = Vector2.Lerp(a, b, (i + 1) / (float)cnt);
                        float gp = Ground(terrain, p.x, p.y), gq = Ground(terrain, q.x, q.y);
                        BuildFencePanel(mb, dummy, FenceKind.Privacy, new Vector3(p.x, gp, p.y), new Vector3(q.x, gq, q.y), wood, 4000 + n * 50 + i);
                        var col = new GameObject("panel"); col.transform.SetParent(holder, false);
                        var mid = (p + q) * 0.5f; float lo = Mathf.Min(gp, gq);
                        // as high as its boards (1.75 m: the ramp's lip is 2.15 m over the deck, a vehicle crossing it must not touch it)
                        col.transform.SetPositionAndRotation(new Vector3(mid.x, lo + 0.775f, mid.y), Quaternion.LookRotation(new Vector3(q.x - p.x, 0f, q.y - p.y), Vector3.up));
                        var bc = col.AddComponent<BoxCollider>(); bc.size = new Vector3(0.14f, 1.95f, Vector2.Distance(p, q) + 0.1f);
                        panels++;
                    }
                    var e = b; float ge = Ground(terrain, e.x, e.y);
                    Post(mb, new Vector3(e.x, ge - 0.2f, e.y), 2.05f, 0.13f, Shade(wood, 0.82f));   // the span's last post
                }
                if (gate)
                {
                    var C = new Vector2(fe.gate.c[0], fe.gate.c[1]); float h = fe.gate.w * 0.5f; var nrm = new Vector2(-dir.y, dir.x);
                    // which side is the lane's (the leaves open toward it): away from the yard's back, i.e. along the line's travel
                    var line = ToPoints(sc.path); int k = Enumerable.Range(0, line.Count - 1).OrderBy(i => (line[i] - C).sqrMagnitude).First();
                    var travel = (line[Mathf.Min(k + 1, line.Count - 1)] - line[k]).normalized; if (Vector2.Dot(travel, nrm) < 0f) nrm = -nrm;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var post = C + dir * (side * h); float g = Ground(terrain, post.x, post.y);
                        Post(mb, new Vector3(post.x, g - 0.2f, post.y), 2.25f, 0.18f, Shade(wood, 0.75f));
                        var pc = new GameObject("gate post"); pc.transform.SetParent(holder, false); pc.transform.position = new Vector3(post.x, g + 0.9f, post.y);
                        var cc = pc.AddComponent<CapsuleCollider>(); cc.radius = 0.12f; cc.height = 2.4f; cc.direction = 1;
                        // the leaf: swung round flat against the fence on the lane's side, boards on a frame
                        var l0 = post + nrm * 0.12f; var l1 = l0 + dir * (side * (h - 0.05f));
                        Beam(mb, new Vector3(l0.x, g + 0.35f, l0.y), new Vector3(l1.x, g + 0.35f, l1.y), 0.05f, 0.1f, Shade(wood, 0.8f), new Vector3(nrm.x, 0f, nrm.y));
                        Beam(mb, new Vector3(l0.x, g + 1.55f, l0.y), new Vector3(l1.x, g + 1.55f, l1.y), 0.05f, 0.1f, Shade(wood, 0.8f), new Vector3(nrm.x, 0f, nrm.y));
                        for (float t = 0.08f; t < h - 0.1f; t += 0.15f)
                        { var bq = l0 + dir * (side * t) + nrm * 0.05f; Beam(mb, new Vector3(bq.x, g + 0.08f, bq.y), new Vector3(bq.x, g + 1.8f, bq.y), 0.13f, 0.025f, Shade(wood, 0.95f), new Vector3(dir.x, 0f, dir.y)); }
                    }
                    Debug.Log($"[Round20+] {sc.id} side gate in fence {fe.label}: {fe.gate.w:0.0} m between round posts at ({C.x:0.0}, {C.y:0.0})");
                }
                mb.ToObject($"{sc.id} fence {n} {fe.label.Split(' ')[0].Replace('/', '-')}", mats.House, holder, false, true);   // its mesh asset's name: no '/'
                n++;
            }
            Debug.Log($"[Round20+] {sc.id}: {n} solid fences ({sc.fences.Count(x => x.kind == "reverse_only")} of them in reverse races only), {panels} panels");
        }

        // patio things (breakable, FenceChunk): white resin chairs, a round table, a striped umbrella, a kettle grill
        static void BuildPatio(ScShortcut sc, TerrainGrid terrain, Materials mats, Transform parent)
        {
            if (sc.props == null || sc.props.Length == 0) return;
            var plastic = new Breakables(); var metal = new Breakables();
            Color white = new Color(0.93f, 0.93f, 0.9f), black = new Color(0.1f, 0.1f, 0.11f), red = new Color(0.78f, 0.16f, 0.15f);
            foreach (var p in sc.props)
            {
                var at = new Vector2(p.x, p.z); float g = Ground(terrain, at.x, at.y) + 0.02f;
                float a = p.yaw * Mathf.Deg2Rad; var fr = MakeFrame(new Vector3(at.x, g, at.y), new Vector2(Mathf.Sin(a), Mathf.Cos(a)), 1f);
                var br = p.kind == "grill" ? metal : plastic; var mb = br.Begin(at);
                switch (p.kind)
                {
                    case "chair":
                        foreach (float u in new[] { -0.28f, 0.28f }) foreach (float v in new[] { -0.25f, 0.25f }) br.Piece(() => BoxF(mb, fr, u - 0.03f, u + 0.03f, 0f, 0.42f, v - 0.03f, v + 0.03f, white), g);
                        br.Piece(() => BoxF(mb, fr, -0.32f, 0.32f, 0.4f, 0.45f, -0.28f, 0.28f, white), g);
                        br.Piece(() => BoxF(mb, fr, -0.32f, 0.32f, 0.45f, 1.0f, -0.32f, -0.27f, white), g);
                        br.End(fr.P(0f, 0.5f, 0f), new Vector3(fr.F.x, 0f, fr.F.z), 0.8f, 1.0f, 0.8f);
                        break;
                    case "table":
                        br.Piece(() => BoxF(mb, fr, -0.05f, 0.05f, 0f, 0.7f, -0.05f, 0.05f, white), g);
                        br.Piece(() => BoxF(mb, fr, -0.55f, 0.55f, 0.7f, 0.74f, -0.55f, 0.55f, white), g);
                        br.End(fr.P(0f, 0.4f, 0f), new Vector3(fr.F.x, 0f, fr.F.z), 1.2f, 0.8f, 1.2f);
                        break;
                    case "umbrella":
                        br.Piece(() => BoxF(mb, fr, -0.3f, 0.3f, 0f, 0.12f, -0.3f, 0.3f, black), g);
                        br.Piece(() => Post(mb, fr.P(0f, 0f, 0f), 2.35f, 0.05f, white), g);
                        br.Piece(() =>
                        {
                            var apex = fr.P(0f, 2.5f, 0f);
                            for (int i = 0; i < 8; i++)
                            {
                                float t0 = Mathf.PI * 2f * i / 8f, t1 = Mathf.PI * 2f * (i + 1) / 8f;
                                var e0 = fr.P(Mathf.Cos(t0) * 1.3f, 2.0f, Mathf.Sin(t0) * 1.3f); var e1 = fr.P(Mathf.Cos(t1) * 1.3f, 2.0f, Mathf.Sin(t1) * 1.3f);
                                mb.Tint = i % 2 == 0 ? red : white; TriFace(mb, apex, e0, e1, Vector3.up + (e0 + e1) * 0.5f - apex);
                                TriFace(mb, apex, e1, e0, -(Vector3.up + (e0 + e1) * 0.5f - apex));
                            }
                        }, g);
                        br.End(fr.P(0f, 1.2f, 0f), new Vector3(fr.F.x, 0f, fr.F.z), 0.7f, 2.4f, 0.7f);
                        break;
                    case "grill":
                        foreach (float u in new[] { -0.22f, 0.22f }) br.Piece(() => BoxF(mb, fr, u - 0.02f, u + 0.02f, 0f, 0.7f, -0.02f, 0.02f, black), g);
                        br.Piece(() => BoxF(mb, fr, -0.32f, 0.32f, 0.65f, 0.95f, -0.3f, 0.3f, black), g);
                        br.Piece(() => BoxF(mb, fr, -0.28f, 0.28f, 0.95f, 1.12f, -0.26f, 0.26f, Shade(black, 1.4f)), g);
                        br.End(fr.P(0f, 0.55f, 0f), new Vector3(fr.F.x, 0f, fr.F.z), 0.8f, 1.1f, 0.8f);
                        break;
                }
            }
            if (plastic.Count > 0) plastic.ToObjects(sc.id + " patio", mats.House, parent, 1f, BreakSounds.Kind.Wood);
            if (metal.Count > 0) metal.ToObjects(sc.id + " grill", mats.House, parent, 1f, BreakSounds.Kind.Mailbox);
            Debug.Log($"[Round20+] {sc.id}: {plastic.Count + metal.Count} breakable patio things");
        }
    }
}
