using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 49 (A, Dan 2026-10-19 08:37: the reverse Chastain Lakes race, "On the bit of land that sticks out before the CP, jump into an intake with into
// a tunnel across the lake and you come out of a manhole just after the 9 cp in the correct direction"): West Chastain Lake's intake, world (it
// stands in Free Roam too), from the 'intake' entry of Data/tracks/chastain_lakes.json (Tools/mapgen/make_lakes_route.py). Its look is its own (the
// forward race's drain is a round morning-glory bell):
//   the jetty   the intake's concrete service jetty off the tip of the point east of Lakeside Point, 6 m wide, 2 m over the water, its end a seawall
//               (solid; off its side or its end you are in the lake)
//   the intake  a square concrete riser in the lake 11.5 m off the jetty's end: walls 0.6 m thick standing 1.2 m out of the water (a car short
//               of them can not climb in from the lake), a weir along their inside edge with the lake pouring through it into the shaft (a round shaft down to LI's open roof, Data/pipes/network.json),
//               steel trash-rack bars on its three far sides (look only), a warning buoy line round it. LakeDrain takes a vehicle that comes
//               in over the weir down the shaft and on into B29 (not a ramp: a scripted drop is allowed for it, the standing rule of 2026-10-17)
//   the hint    the lake association's LAKE INTAKE sign at the service track's mouth on Lakeside Drive (Tools/handmade/make_intake_sign.py), worn
//               ruts up the track; hidden in a forward race, when the intake's grate is on and LakeDrain is shut (the forward race's barrier
//               across the track is the route's)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class LakesIntakeData
        {
            public float[] at, edge, dir, exit, sign, tracks;
            public float pad_len, pad_half, pad_y, water_y, catch_r, shaft_r, wall, wall_up;
            public string chamber, shut_forward_of;
        }

        static void BuildLakeIntake(TerrainGrid terrain, Materials mats, Transform root, LakesIntakeData ix)
        {
            if (ix == null || ix.at == null || ix.at.Length < 2) return;
            var net = Net; NetChamber li = null;
            if (net != null && net.F.chambers != null) li = net.F.chambers.FirstOrDefault(c => c.id == ix.chamber);
            if (li == null || !net.Built(li)) { Debug.LogWarning($"[Round49] the intake's chamber {ix.chamber} is not built: no intake"); return; }
            var go = new GameObject("Lake intake").transform; go.SetParent(root, false);
            var c2 = new Vector2(ix.at[0], ix.at[1]); var edge = new Vector2(ix.edge[0], ix.edge[1]); var d = new Vector2(ix.dir[0], ix.dir[1]).normalized; var nl = new Vector2(-d.y, d.x);
            float wy = Beds != null ? WaterSurfaceAt(c2) : float.NaN; if (float.IsNaN(wy)) wy = ix.water_y;
            var concrete = new Color(0.64f, 0.63f, 0.59f); var wet = new Color(0.38f, 0.39f, 0.36f); var dark = new Color(0.19f, 0.20f, 0.20f); var steel = new Color(0.30f, 0.31f, 0.32f);

            // the jetty: a solid concrete slab from the point's tip out to the seawall, level; no face at its root (flush with the track)
            var root2 = edge - d * ix.pad_len;
            var jetty = new ScPrism
            {
                name = "sc LI jetty", kind = "pad", no_face = new[] { 0 }, origin = new[] { root2.x, root2.y }, axis = new[] { d.x, d.y },
                profile = new[] { 0f, ix.pad_y, ix.pad_len, ix.pad_y }, colour = new[] { concrete.r, concrete.g, concrete.b },
                poly = new[] { root2 + nl * ix.pad_half, root2 - nl * ix.pad_half, edge - nl * ix.pad_half, edge + nl * ix.pad_half }.SelectMany(q => new[] { q.x, q.y }).ToArray()
            };
            BuildPrism(jetty, terrain, mats, go);

            // the riser: four walls round the shaft (solid), their tops 0.35 m out of the water; the weir's lip inside them a little lower
            // the walls stand 'wall_up' out of the water (1.2 m: too high to climb out of the lake; off the jetty under about 16 m/s you hit them)
            float top = wy + (ix.wall_up > 0f ? ix.wall_up : 0.35f), weir = wy + 0.12f, bed = Ground(terrain, c2.x, c2.y), ceil = li.floor + li.height, W = ix.wall, In = ix.catch_r, r = ix.shaft_r;
            var walls = new MeshBuilder(); var look = new MeshBuilder();
            Vector3 At(float u, float v, float y) { var q = c2 + nl * u + d * v; return new Vector3(q.x, y, q.y); }
            // the walls as four boxes (outer W, inner In), down to the bed and a little under it
            foreach (var (u0, u1, v0, v1) in new[] { (-W, W, In, W), (-W, W, -W, -In), (-W, -In, -In, In), (In, W, -In, In) })
            {
                walls.Tint = concrete;
                Face(walls, At(u0, v0, top), At(u1, v0, top), At(u1, v1, top), At(u0, v1, top), Vector3.up);
                walls.Tint = wet;
                Face(walls, At(u0, v1, bed - 0.6f), At(u1, v1, bed - 0.6f), At(u1, v1, top), At(u0, v1, top), new Vector3(d.x, 0f, d.y));
                Face(walls, At(u1, v0, bed - 0.6f), At(u0, v0, bed - 0.6f), At(u0, v0, top), At(u1, v0, top), new Vector3(-d.x, 0f, -d.y));
                Face(walls, At(u1, v1, bed - 0.6f), At(u1, v0, bed - 0.6f), At(u1, v0, top), At(u1, v1, top), new Vector3(nl.x, 0f, nl.y));
                Face(walls, At(u0, v0, bed - 0.6f), At(u0, v1, bed - 0.6f), At(u0, v1, top), At(u0, v0, top), new Vector3(-nl.x, 0f, -nl.y));
            }
            walls.ToObject("Lake intake walls", mats.House, go, true);
            // inside: the weir's chamfer from the walls' inner edge down to the shaft, the shaft (round, solid) to just under LI's ceiling, a ladder
            const int N = 40;
            Vector3 Ring(float rad, float a, float y) => new Vector3(c2.x + Mathf.Cos(a) * rad, y, c2.y + Mathf.Sin(a) * rad);
            var sh = new MeshBuilder();
            for (int i = 0; i < N; i++)
            {
                float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N; var om = new Vector3(Mathf.Cos((a0 + a1) / 2f), 0f, Mathf.Sin((a0 + a1) / 2f));
                // the square's inner edge at this angle (a square of half In), down to the shaft's rim
                Vector3 Sq(float a, float y) { float cx = Mathf.Cos(a), sz = Mathf.Sin(a), k = In / Mathf.Max(Mathf.Abs(Vector2.Dot(new Vector2(cx, sz), nl)), Mathf.Abs(Vector2.Dot(new Vector2(cx, sz), d))); return Ring(k, a, y); }
                look.Tint = wet; Face(look, Sq(a0, weir), Sq(a1, weir), Ring(r, a1, weir - 0.7f), Ring(r, a0, weir - 0.7f), (Vector3.up - om).normalized);
                sh.Tint = wet; Face(sh, Ring(r, a0, weir - 0.7f), Ring(r, a1, weir - 0.7f), Ring(r, a1, weir - 3f), Ring(r, a0, weir - 3f), -om);
                sh.Tint = dark; Face(sh, Ring(r, a0, weir - 3f), Ring(r, a1, weir - 3f), Ring(r, a1, ceil - 0.05f), Ring(r, a0, ceil - 0.05f), -om);
                look.Tint = new Color(0.55f, 0.54f, 0.50f); Face(look, Ring(r, a0, ceil - 0.03f), Ring(r, a1, ceil - 0.03f), Ring(5.4f, a1, ceil - 0.03f), Ring(5.4f, a0, ceil - 0.03f), Vector3.down);
                if (i == 7) for (float y = weir - 0.9f; y > ceil + 0.4f; y -= 0.45f) Beam(look, Ring(r - 0.15f, a0 + 0.02f, y), Ring(r - 0.15f, a1 - 0.02f, y), 0.04f, 0.04f, new Color(0.25f, 0.22f, 0.20f));
            }
            sh.ToObject("Lake intake shaft", mats.House, go, true, false);
            // the trash rack: steel bars standing on the three far walls' tops (look only; the jetty side is open), a top rail; a gauge board on one
            foreach (var (a, b) in new[] { (new Vector2(-W + 0.3f, W - 0.3f), new Vector2(W - 0.3f, W - 0.3f)), (new Vector2(-W + 0.3f, -W + 0.3f), new Vector2(-W + 0.3f, W - 0.3f)), (new Vector2(W - 0.3f, -W + 0.3f), new Vector2(W - 0.3f, W - 0.3f)) })
            {
                float L = Vector2.Distance(a, b);
                for (float t = 0f; t <= L + 1e-3f; t += 0.35f) { var q = Vector2.Lerp(a, b, t / L); Beam(look, At(q.x, q.y, top), At(q.x, q.y, top + 1.1f), 0.035f, 0.035f, steel); }
                Beam(look, At(a.x, a.y, top + 1.1f), At(b.x, b.y, top + 1.1f), 0.07f, 0.07f, steel);
            }
            for (int k = 0; k < 8; k++) { look.Tint = k % 2 == 0 ? Color.white : new Color(0.85f, 0.15f, 0.12f); var p0 = At(W - 0.35f, -W + 0.6f, top + 0.1f + k * 0.25f); Face(look, p0, p0 + new Vector3(0f, 0.25f, 0f), p0 + new Vector3(nl.x * 0.01f, 0.25f, nl.y * 0.01f) + new Vector3(d.x, 0f, d.y) * 0.3f, p0 + new Vector3(d.x, 0f, d.y) * 0.3f, new Vector3(nl.x, 0f, nl.y)); }
            // the warning buoys: a ring of white and orange floats on a line 4 m out from the walls, open toward the jetty
            for (int k = 0; k < 18; k++)
            {
                float a = Mathf.Lerp(-150f, 150f, k / 17f) * Mathf.Deg2Rad; var o = c2 + (d * Mathf.Cos(a) + nl * Mathf.Sin(a)) * (W + 4f);
                LookCyl(look, new Vector3(o.x, wy - 0.05f, o.y), new Vector3(o.x, wy + 0.32f, o.y), 0.22f, k % 2 == 0 ? Color.white : new Color(1f, 0.45f, 0.08f), 8);
            }
            var lg = look.ToObject("Lake intake look", mats.House, go, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            // the terrain cut away inside the shaft (the lake bed over LI's open roof)
            int cut = SetHoles(terrain, new Rect(c2.x - W, c2.y - W, 2f * W, 2f * W), q => (q - c2).magnitude < r + 0.4f);

            // the drop (LakeDrain), the water pouring over the weir
            var ex = new Vector3(ix.exit[0], ix.exit[1], ix.exit[2]);
            var dr = go.gameObject.AddComponent<LakeDrain>();
            dr.mouth = new Vector3(c2.x, wy, c2.y); dr.radius = In; dr.shaftRadius = r; dr.floorY = li.floor; dr.exit = ex; dr.curtain = false;
            dr.shutForwardOf = ix.shut_forward_of; dr.fromAirOnly = true;   // a vehicle that lands short in the lake and wades up to it is not taken
            var spray = SpraySoftMaterial != null ? SpraySoftMaterial : MakeSprayMaterial();
            foreach (var (u, v, du, dv) in new[] { (0f, In, 1f, 0f), (0f, -In, 1f, 0f), (In, 0f, 0f, 1f), (-In, 0f, 0f, 1f) })
            {
                var w = new GameObject("Lake intake weir water"); w.transform.SetParent(go, false);
                var mid = At(u, v, weir + 0.05f); var inward = (new Vector3(c2.x, mid.y, c2.y) - mid).normalized;
                w.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(inward, Vector3.up));
                var ps = w.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.loop = true; main.startLifetime = 1.1f; main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f); main.startColor = new Color(0.86f, 0.92f, 0.96f, 0.7f);
                main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 900; main.gravityModifier = 1f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
                var em = ps.emission; em.rateOverTime = 260f;
                var shp = ps.shape; shp.shapeType = ParticleSystemShapeType.Box; shp.scale = new Vector3(2f * In, 0.05f, 0.05f);
                var rd = w.GetComponent<ParticleSystemRenderer>(); rd.sharedMaterial = spray; rd.renderMode = ParticleSystemRenderMode.Billboard; rd.shadowCastingMode = ShadowCastingMode.Off; rd.receiveShadows = false;
                ps.Play();
            }

            // shut in a forward race: a steel grate over the shaft at the weir (solid, drivable), the clues hidden
            var clues = go;
            if (!string.IsNullOrEmpty(ix.shut_forward_of))
            {
                var hide = new GameObject("Lake intake clues (hidden in a forward race)"); hide.transform.SetParent(go, false);
                hide.AddComponent<RaceOnly>().hideInForwardOf = ix.shut_forward_of; clues = hide.transform;
                var show = new GameObject("Lake intake grate (a forward race only)"); show.transform.SetParent(go, false);
                show.AddComponent<RaceOnly>().showInForwardOf = ix.shut_forward_of;
                var gm = new MeshBuilder(); float gy = top + 0.04f;
                for (float t = -In + 0.3f; t < In; t += 0.5f)
                {
                    Beam(gm, At(t, -In, gy), At(t, In, gy), 0.06f, 0.06f, steel);
                    Beam(gm, At(-In, t, gy + 0.08f), At(In, t, gy + 0.08f), 0.06f, 0.06f, steel);
                }
                var gl = gm.ToObject("Lake intake grate", mats.House, show.transform, false); gl.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                var plate = new MeshBuilder { Tint = new Color(0.16f, 0.17f, 0.18f) };
                Face(plate, At(-In, -In, gy - 0.02f), At(In, -In, gy - 0.02f), At(In, In, gy - 0.02f), At(-In, In, gy - 0.02f), Vector3.up);
                plate.ToObject("Lake intake grate plate", mats.House, show.transform, true, false);
            }
            if (ix.sign != null && ix.sign.Length >= 3) IntakeSign(terrain, mats, clues, new Vector2(ix.sign[0], ix.sign[1]), ix.sign[2]);
            if (ix.tracks != null && ix.tracks.Length >= 6) WornRuts(terrain, mats, clues, ToPoints(ix.tracks), "Lake intake worn tracks");
            Debug.Log($"[Round49] the lake intake at ({c2.x:0.0}, {c2.y:0.0}): the water {wy:0.00} m, its walls {top:0.00} m ({2f * W:0.0} m square), the weir {weir:0.00} m ({2f * In:0.0} m inside), " +
                      $"the shaft {2f * r:0.0} m down {weir - ceil:0.0} m to {li.id}'s roof ({ceil:0.00} m), its floor {li.floor:0.00} m; the jetty {ix.pad_len:0} x {2f * ix.pad_half:0} m at {ix.pad_y:0.00} m " +
                      $"({ix.pad_y - wy:0.0} m over the water) to ({edge.x:0.0}, {edge.y:0.0}); {cut} terrain cells cut; on into B29 at ({ex.x:0.0}, {ex.y:0.0}, {ex.z:0.0})");
        }

        // the lake association's sign: two steel posts, the aluminum board (2.4 x 1.6 m, its face Tools/handmade/make_intake_sign.py, arrow right); breaks
        static void IntakeSign(TerrainGrid terrain, Materials mats, Transform parent, Vector2 at, float yawDeg)
        {
            var facing = new Vector2(Mathf.Sin(yawDeg * Mathf.Deg2Rad), Mathf.Cos(yawDeg * Mathf.Deg2Rad));
            var f = MakeFrame(new Vector3(at.x, 0f, at.y), facing, 1f);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (float u in new[] { -1.1f, 1.1f }) { var q = f.P(u, 0f, -0.05f); float g = Ground(terrain, q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            f.O.y = hi;
            var br = new Breakables(); var mb = br.Begin(at);
            var post = new Color(0.42f, 0.43f, 0.44f); var back = new Color(0.70f, 0.71f, 0.72f);
            foreach (float u in new[] { -1.0f, 1.0f }) br.Piece(() => BoxF(mb, f, u - 0.05f, u + 0.05f, lo - hi - 0.3f, 2.7f, -0.1f, -0.01f, post), lo + 0.05f);
            br.Piece(() => BoxF(mb, f, -1.2f, 1.2f, 1.0f, 2.6f, -0.01f, 0.02f, back), lo + 0.05f);
            string path = "Assets/HandMade/Shortcut/IntakeSignRight.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) Debug.LogWarning("[Round49] " + path + " missing (Tools/handmade/make_intake_sign.py): no intake sign face");
            else
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "IntakeSign" };
                m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.2f); m.SetFloat("_Cull", (float)CullMode.Back);
                m = GenAsset(m, GeneratedDir + "/IntakeSign.mat");
                var plate = SignPlate(parent, f.P(0f, 1.8f, 0.025f), -f.F, 1f, m);
                plate.name = "Intake sign face"; plate.transform.localScale = new Vector3(2.4f, 1.6f, 1f);
                br.Face(plate);
            }
            br.End(f.P(0f, 1.9f, -0.03f), f.F, 2.6f, 3.2f, 0.6f);
            br.ToObjects("Intake sign", mats.House, parent, 1f, BreakSounds.Kind.Sign);
            Debug.Log($"[Round49] the intake's LAKE INTAKE sign at ({at.x:0.0}, {at.y:0.0}), facing {yawDeg:0}");
        }
    }
}
