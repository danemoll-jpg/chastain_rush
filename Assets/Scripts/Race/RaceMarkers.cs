using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 5: what the driver sees of the route. Everything here is scenery with no colliders, built at runtime from the
    // route so a direction change needs no rebuild.
    //   start / finish: a black-and-white chequered arch across the road. Round 12 (BUG-006, Dan: "start lines should be
    //   non-existent in free roam"): shown only while a race is running, like everything else here;
    //   checkpoints: an arch each, shown only during a race. The NEXT one is bright green with two tall beams you can see
    //   over hills and trees from far down the road; later ones are amber arches; ones skipped this lap are red; passed ones are hidden.
    //   Round 8, official shortcuts: a BLUE arch with two tall blue beams over the shortcut's entry plane, and a blue chevron
    //   sign on the verge where its line leaves the road, pointing the way. Shown during a race while the shortcut can still
    //   be taken this lap (the checkpoints it forgives are not yet passed); dimmed (no beams) once it is of no use.
    //   (Racer marks its shortcuts with a dirt trail painted into the ground and no arch; the trail is built into the map here
    //   too, MapBuilder.Shortcuts.cs; the arch and chevron are this game's checkpoint look in the shortcut colour.)
    //   Round 11 (Dan: shortcuts are found, not signposted): the shortcut arch, beams and chevron are no longer built. The
    //   code stays for reference; nothing marks a shortcut.
    public sealed class RaceMarkers : MonoBehaviour
    {
        class Arch { public GameObject root; public Renderer[] paint; public GameObject beams; public RaceGate gate; }
        readonly List<Arch> arches = new List<Arch>();
        readonly List<(Arch arch, GameObject sign, RaceShortcut sc)> shortcutMarks = new List<(Arch, GameObject, RaceShortcut)>();
        Arch finish;
        static readonly bool MarkShortcuts = false;   // Round 11: off (no arches, beams or signs on shortcuts)
        RaceController race;
        Material fallbackWhite, fallbackBlack, fallbackNext, fallbackLater, fallbackMissed, fallbackShortcut;

        public void Build(RaceController r)
        {
            race = r;
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            arches.Clear(); shortcutMarks.Clear();
            finish = MakeFinish(r.Route.Gate);
            foreach (var g in r.Route.Checkpoints) arches.Add(MakeCheckpoint(g));
            if (MarkShortcuts) foreach (var sc in r.Route.Shortcuts) shortcutMarks.Add((MakeShortcutEntry(sc), MakeTurnSign(r.Route, sc), sc));
        }

        public void Refresh(RaceController r)
        {
            bool racing = r.State != RaceController.Stage.Free;
            if (finish != null) finish.root.SetActive(racing);   // Round 12 (BUG-006)
            for (int i = 0; i < arches.Count; i++)
            {
                var a = arches[i];
                bool finished = r.State == RaceController.Stage.Finished;
                bool passed = r.ClockStarted && i < r.NextCheckpoint && !r.MissedThisLap.Contains(i);
                bool missedNow = r.ClockStarted && r.MissedThisLap.Contains(i);
                bool next = r.ClockStarted && i == r.NextCheckpoint && !finished;
                bool visible = racing && !finished && !passed;
                // before the clock starts the whole lap is previewed: the first checkpoint is already the lit one
                if (racing && !r.ClockStarted) { visible = true; next = i == 0; }
                a.root.SetActive(visible);
                if (!visible) continue;
                var mat = missedNow ? Missed : next ? Next : Later;
                foreach (var rd in a.paint) rd.sharedMaterial = mat;
                a.beams.SetActive(next);
            }
            foreach (var (arch, sign, sc) in shortcutMarks)
            {
                bool show = racing && r.State != RaceController.Stage.Finished;
                bool useful = show && sc.LastForgiven >= r.NextCheckpoint;
                arch.root.SetActive(show);
                if (sign) sign.SetActive(show);
                arch.beams.SetActive(useful);
            }
        }

        // ------------------------------------------------------------ pieces

        Arch MakeShortcutEntry(RaceShortcut sc)
        {
            var g = sc.Entry;
            var root = new GameObject("Shortcut entry " + sc.Id);
            root.transform.SetParent(transform, false);
            root.transform.SetPositionAndRotation(g.Position, Quaternion.LookRotation(g.Forward, Vector3.up));
            var paint = new List<Renderer>();
            float h = 6.5f, half = Mathf.Min(g.HalfWidth, 5.5f);
            paint.Add(Box(root.transform, new Vector3(-half, h / 2f, 0), new Vector3(0.7f, h, 0.7f), Shortcut));
            paint.Add(Box(root.transform, new Vector3(half, h / 2f, 0), new Vector3(0.7f, h, 0.7f), Shortcut));
            paint.Add(Box(root.transform, new Vector3(0, h - 0.5f, 0), new Vector3(half * 2f, 1.0f, 0.4f), Shortcut));
            var beams = new GameObject("Beams"); beams.transform.SetParent(root.transform, false);
            foreach (float side in new[] { -1f, 1f })
                paint.Add(Box(beams.transform, new Vector3(side * half, 40f, 0), new Vector3(0.8f, 80f, 0.8f), Shortcut));
            return new Arch { root = root, paint = paint.ToArray(), beams = beams, gate = g };
        }

        // A chevron sign on the verge where the shortcut's line leaves the road, facing the oncoming driver, pointing its way.
        GameObject MakeTurnSign(RaceRoute route, RaceShortcut sc)
        {
            Vector3 leave = sc.Path[0]; float bestS = 0f;
            for (int i = 0; i < sc.Path.Length; i++)
            {
                float d = float.MaxValue, sAt = 0f;
                for (float s = 0; s < route.LapLength; s += 2f) { var q = route.PointAt(s, out _); float dd = Flat(q - sc.Path[i]).sqrMagnitude; if (dd < d) { d = dd; sAt = s; } }
                if (Mathf.Sqrt(d) > 6.5f) { leave = sc.Path[i]; bestS = sAt; break; }
            }
            var p = route.PointAt(bestS, out var fwd);
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            float side = Mathf.Sign(Vector3.Dot(leave - p, right));
            var root = new GameObject("Shortcut sign " + sc.Id);
            root.transform.SetParent(transform, false);
            var at = RaceRoute.Surface(leave.x, leave.z) - fwd * 6f;   // a little before the turn-off (Round 27: Surface)
            root.transform.SetPositionAndRotation(at, Quaternion.LookRotation(fwd, Vector3.up));
            Box(root.transform, new Vector3(0, 1.2f, 0), new Vector3(0.25f, 2.4f, 0.25f), White);
            Box(root.transform, new Vector3(0, 3.3f, 0.05f), new Vector3(3.4f, 1.8f, 0.15f), White);
            for (int k = 0; k < 2; k++)
            {
                float x = (-0.7f + k * 1.1f) * side;
                foreach (float up in new[] { -1f, 1f })
                {
                    var arm = Box(root.transform, new Vector3(x, 3.3f + up * 0.32f, -0.05f), new Vector3(0.95f, 0.28f, 0.1f), Shortcut).transform;
                    arm.localRotation = Quaternion.Euler(0f, 0f, -up * side * 40f);
                }
            }
            return root;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        Arch MakeCheckpoint(RaceGate g)
        {
            var root = Frame(g, "Checkpoint " + g.Id);
            var paint = new List<Renderer>();
            float h = g.Arch > 0f ? g.Arch : 7.5f;   // Round 27: low in a pipe
            paint.Add(Box(root.transform, new Vector3(-g.HalfWidth, h / 2f, 0), new Vector3(0.9f, h, 0.9f)));
            paint.Add(Box(root.transform, new Vector3(g.HalfWidth, h / 2f, 0), new Vector3(0.9f, h, 0.9f)));
            paint.Add(Box(root.transform, new Vector3(0, h - 0.6f, 0), new Vector3(g.HalfWidth * 2f, 1.2f, 0.5f)));
            var beams = new GameObject("Beams"); beams.transform.SetParent(root.transform, false);
            if (!(RaceController.Instance && RaceController.Instance.Route != null && RaceController.Instance.Route.Underground))   // Round 27: no beams through the ground over a pipe
                foreach (float side in new[] { -1f, 1f })
                    paint.Add(Box(beams.transform, new Vector3(side * g.HalfWidth, 40f, 0), new Vector3(1.0f, 80f, 1.0f)));
            return new Arch { root = root, paint = paint.ToArray(), beams = beams, gate = g };
        }

        Arch MakeFinish(RaceGate g)
        {
            var root = Frame(g, "Start-finish gate");
            var rdata = RaceController.Instance ? RaceController.Instance.RouteData : null;
            float h = rdata != null && rdata.gate_arch_m > 0f ? rdata.gate_arch_m : 7.5f;   // Round 27: under the hub's ceiling
            if (rdata != null && rdata.gate_ring_m > 0f)
            {
                // Round 35: the line in a pipe (the Underground race, M5's straight): a checkered band round the pipe's wall, two rows deep
                float r = rdata.gate_ring_m - 0.15f; const int n = 24; float seg = 2f * Mathf.PI * r / n + 0.02f;
                for (int k = 0; k < n; k++)
                    for (int row = 0; row < 2; row++)
                    {
                        float a = (k + 0.5f) * 2f * Mathf.PI / n;
                        var c = Box(root.transform, new Vector3(Mathf.Sin(a) * r, rdata.gate_ring_m - Mathf.Cos(a) * r, (row - 0.5f) * 0.9f), new Vector3(seg, 0.3f, 0.9f), ((k + row) % 2 == 0) ? Black : White);
                        c.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
                    }
                return new Arch { root = root, paint = new Renderer[0], beams = new GameObject("none"), gate = g };
            }
            // Round 70 (F3, Dan's BUG-001 on Kennesaw Mountain Drive: the banner's end down in the hillside): the gantry stands upright, each post
            // on the ground where it stands (closer in where the ground beside the road climbs more than 2.5 m over the line), the banner level
            // and h clear over the higher of the line and the posts' ground. Off the terrain (a bridge, the mall, a pipe) as before.
            float[] px = { -g.HalfWidth, g.HalfWidth }, py = { 0f, 0f };
            float gc = GroundQuery.Height(g.Position.x, g.Position.z);
            if (!float.IsNaN(gc) && Mathf.Abs(gc - g.Position.y) < 1f && !(RaceRoute.Active != null && RaceRoute.Active.Underground))
            {
                var flat = Flat(g.Forward); if (flat.sqrMagnitude < 1e-4f) flat = Vector3.forward;
                root.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
                var right = root.transform.right;
                for (int s = 0; s < 2; s++)
                {
                    float sd = s == 0 ? -1f : 1f, d = 2f, y = 0f;
                    for (float dd = 2f; dd <= g.HalfWidth + 1e-3f; dd += .5f)
                    {
                        var q = g.Position + right * sd * dd; float qy = GroundQuery.Height(q.x, q.z); if (float.IsNaN(qy)) break;
                        if (qy - g.Position.y > 2.5f) break;
                        d = dd; y = qy - g.Position.y;
                    }
                    px[s] = sd * d; py[s] = y;
                }
            }
            float top = Mathf.Max(0f, Mathf.Max(py[0], py[1])) + h;   // the banner's top
            for (int s = 0; s < 2; s++)
            {
                // chequered pylons: alternating black and white blocks, from 0.3 m into the ground up to the banner
                float b = py[s] - .3f; int n = Mathf.Max(1, Mathf.CeilToInt((top - .3f - b) / 1.2f));
                for (int k = 0; k < n; k++)
                    Box(root.transform, new Vector3(px[s], top - .3f - 0.6f - k * 1.2f, 0), new Vector3(1.0f, 1.2f, 1.0f), ((n - 1 - k) % 2 == 0) == (s == 1) ? Black : White);
            }
            float span = px[1] - px[0];
            int cells = Mathf.Max(2, Mathf.RoundToInt(span / 1.5f));
            float cw = span / cells;
            for (int c = 0; c < cells; c++)
                for (int row = 0; row < 2; row++)
                    Box(root.transform, new Vector3(px[0] + cw * (c + 0.5f), top - 0.6f - row * 0.9f + 0.45f, 0), new Vector3(cw, 0.9f, 0.5f), ((c + row) % 2 == 0) ? Black : White);
            return new Arch { root = root, paint = new Renderer[0], beams = new GameObject("none"), gate = g };
        }

        // An empty object on the ground at the gate, turned to its travel direction (local +z = forward, +x = right).
        GameObject Frame(RaceGate g, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(g.Position, Quaternion.LookRotation(g.Forward, Vector3.up));
            return go;
        }

        Renderer Box(Transform parent, Vector3 localPos, Vector3 size, Material mat = null)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(c.GetComponent<Collider>()); // markers are only to look at: the car drives through nothing
            c.transform.SetParent(parent, false);
            c.transform.localPosition = localPos; c.transform.localScale = size;
            var rd = c.GetComponent<Renderer>();
            rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rd.receiveShadows = false;
            if (mat != null) rd.sharedMaterial = mat;
            return rd;
        }

        // Materials come from the scene (MapBuilder made them as assets); if a scene predates them, plain unlit ones are made.
        Material White => race.gateWhite ? race.gateWhite : (fallbackWhite ??= Unlit(new Color(0.95f, 0.95f, 0.95f)));
        Material Black => race.gateBlack ? race.gateBlack : (fallbackBlack ??= Unlit(new Color(0.05f, 0.05f, 0.05f)));
        Material Next => race.nextMaterial ? race.nextMaterial : (fallbackNext ??= Unlit(new Color(0.35f, 1f, 0.2f)));
        Material Later => race.laterMaterial ? race.laterMaterial : (fallbackLater ??= Unlit(new Color(1f, 0.62f, 0.1f)));
        Material Missed => race.missedMaterial ? race.missedMaterial : (fallbackMissed ??= Unlit(new Color(0.9f, 0.1f, 0.1f)));
        Material Shortcut => race.shortcutMaterial ? race.shortcutMaterial : (fallbackShortcut ??= Unlit(new Color(0.15f, 0.65f, 1f)));
        static Material Unlit(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var m = new Material(sh);
            m.SetColor("_BaseColor", c); m.color = c;
            return m;
        }
    }
}
