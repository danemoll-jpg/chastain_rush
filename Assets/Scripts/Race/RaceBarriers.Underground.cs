using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 27 (Part D, the underground race; Dan, Round 23: "during underground races we can [have] gates sealing off other [areas] and
    // potential shortcuts would require you to drive over a switch or something to open them"). The race layer, built from the route's
    // data only while that race runs (free roam: the network is open and none of this is there):
    //   seals         a barred steel grille across every pipe the race does not use where it opens into the race's junctions and the hub
    //   rails         railings along the hub's top ledge (none there otherwise: "falling off is part of the fun") so the way down is the
    //                 drop beyond M5's mouth, or the shortcut
    //   switch gates  a grille across a shortcut, with a yellow plate somewhere off the racing line: the first racer (the player or an AI)
    //                 to drive over the plate opens the grille for everyone for the rest of the race (it rises into the roof, the plate's
    //                 lamp turns green)
    public sealed partial class RaceBarriers
    {
        public sealed class SwitchGate
        {
            public RouteSwitchGateJson Data; public Transform Grille; public Renderer Lamp; public BoxCollider Block;
            public bool Open; public float OpenedAt; public Vector3 Closed; public string OpenedBy;
        }
        public readonly List<SwitchGate> Gates = new List<SwitchGate>();
        public int Seals, Rails;
        Material steel, hazard, dark, lampRed, lampGreen, railMat;

        void BuildUnderground(RaceRoute route)
        {
            var j = route.Data; if (j == null) return;
            if (steel == null)
            {
                steel = Unlit(new Color(0.20f, 0.22f, 0.24f)); hazard = Unlit(new Color(0.95f, 0.75f, 0.05f)); dark = Unlit(new Color(0.06f, 0.06f, 0.06f));
                lampRed = Unlit(new Color(1f, 0.12f, 0.08f)); lampGreen = Unlit(new Color(0.15f, 1f, 0.3f)); railMat = Unlit(new Color(0.85f, 0.62f, 0.05f));
            }
            if (j.seals != null) foreach (var s in j.seals) { Grille(s, "Seal " + s.id, false); Seals++; }
            if (j.rails != null) foreach (var r in j.rails) { Rail(r); Rails++; }
            if (j.switch_gates != null)
                foreach (var sg in j.switch_gates)
                {
                    var g = new SwitchGate { Data = sg };
                    var root = Grille(sg.gate, "Switch gate " + sg.id, true);
                    g.Grille = root.transform; g.Closed = root.transform.position; g.Block = root.GetComponent<BoxCollider>();
                    g.Lamp = Plate(sg);
                    Gates.Add(g);
                }
        }

        const float WallTop = 3.0f;   // Round 57 (D): how high a rail's or a grille's collider reaches over its foot

        // a barred grille: its foot at (x, y, z), across the way (dx, dz), w wide and h high, solid
        GameObject Grille(RouteSealJson s, string name, bool striped)
        {
            var root = new GameObject(name); root.transform.SetParent(transform, false);
            var along = new Vector3(s.dx, 0f, s.dz).normalized; if (along.sqrMagnitude < 0.5f) along = Vector3.forward;
            root.transform.SetPositionAndRotation(new Vector3(s.x, s.y, s.z), Quaternion.LookRotation(along, Vector3.up));
            // Round 57 (D, Dan BUG-005 at the hub: "not enough to prevent you from going over the railing or under it"): solid from 0.3 m under its foot
            // to 3 m (or its own height) above it and 0.6 m thick, so a car at race speed neither climbs it nor slips under it
            float top = Mathf.Max(s.h, WallTop);
            var box = root.AddComponent<BoxCollider>(); box.size = new Vector3(s.w, top + 0.3f, 0.6f); box.center = new Vector3(0f, (top - 0.3f) * 0.5f, 0f);
            void Bar(Vector3 c, Vector3 size, Material m)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(b.GetComponent<Collider>());
                b.transform.SetParent(root.transform, false); b.transform.localPosition = c; b.transform.localScale = size;
                var rd = b.GetComponent<Renderer>(); rd.sharedMaterial = m; rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            int n = Mathf.Max(3, Mathf.RoundToInt(s.w / 0.4f));
            for (int i = 0; i <= n; i++) Bar(new Vector3(-s.w * 0.5f + s.w * i / n, s.h * 0.5f, 0f), new Vector3(0.09f, s.h, 0.09f), steel);
            for (int k = 0; k < 4; k++)
            {
                float y = s.h * (k + 0.5f) / 4f;
                Bar(new Vector3(0f, y, 0f), new Vector3(s.w, 0.12f, 0.14f), striped && k % 2 == 0 ? hazard : steel);
            }
            Bar(new Vector3(0f, 0.08f, 0f), new Vector3(s.w, 0.16f, 0.2f), striped ? hazard : dark);
            return root;
        }

        // a railing along a polyline at height y (Round 58: or each point's own, ys): posts every 2.5 m, a top and a middle rail, solid
        void Rail(RouteRailJson r)
        {
            var root = new GameObject("Rail " + r.id); root.transform.SetParent(transform, false);
            float h = r.height > 0f ? r.height : 1.1f;
            for (int i = 0; i + 3 < r.points.Length; i += 2)
            {
                // Round 58 (B): a rail up the span's side has each point's own height
                bool own = r.ys != null && r.ys.Length * 2 == r.points.Length;
                var a = new Vector3(r.points[i], own ? r.ys[i / 2] : r.y, r.points[i + 1]); var b = new Vector3(r.points[i + 2], own ? r.ys[i / 2 + 1] : r.y, r.points[i + 3]);
                var d = b - a; float len = d.magnitude; if (len < 0.05f) continue;
                var seg = new GameObject("segment"); seg.transform.SetParent(root.transform, false);
                seg.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.LookRotation(d / len, Vector3.up));
                // Round 57 (D): solid from 0.3 m under the ledge to 3 m over it (was 1.4 m) and 0.6 m thick; a third rail low down shows the way under is shut
                float top = Mathf.Max(h + 0.3f, WallTop);
                var box = seg.AddComponent<BoxCollider>(); box.size = new Vector3(0.6f, top + 0.3f, len + 0.3f); box.center = new Vector3(0f, (top - 0.3f) * 0.5f, 0f);
                foreach (float y in new[] { h, h * 0.5f, 0.2f })
                {
                    var rail = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(rail.GetComponent<Collider>());
                    rail.transform.SetParent(seg.transform, false); rail.transform.localPosition = new Vector3(0f, y, 0f); rail.transform.localScale = new Vector3(0.1f, 0.1f, len);
                    var rd = rail.GetComponent<Renderer>(); rd.sharedMaterial = railMat; rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                int posts = Mathf.Max(1, Mathf.CeilToInt(len / 2.5f));
                for (int k = 0; k <= posts; k++)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(post.GetComponent<Collider>());
                    post.transform.SetParent(seg.transform, false); post.transform.localPosition = new Vector3(0f, h * 0.5f, -len * 0.5f + len * k / posts); post.transform.localScale = new Vector3(0.12f, h, 0.12f);
                    var rd = post.GetComponent<Renderer>(); rd.sharedMaterial = steel; rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        // the switch: a yellow plate with black edges, lying on the surface (its normal n), and a lamp beside it (red, green when open)
        Renderer Plate(RouteSwitchGateJson sg)
        {
            var root = new GameObject("Switch " + sg.id); root.transform.SetParent(transform, false);
            var n = new Vector3(sg.nx, sg.ny, sg.nz); if (n.sqrMagnitude < 0.5f) n = Vector3.up; n.Normalize();
            var fwd = Vector3.Cross(n, Vector3.Cross(Vector3.forward, n)); if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.Cross(n, Vector3.right); fwd.Normalize();
            root.transform.SetPositionAndRotation(new Vector3(sg.sx, sg.sy, sg.sz), Quaternion.LookRotation(fwd, n));
            Renderer Part(Vector3 c, Vector3 size, Material m)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(b.GetComponent<Collider>());
                b.transform.SetParent(root.transform, false); b.transform.localPosition = c; b.transform.localScale = size;
                var rd = b.GetComponent<Renderer>(); rd.sharedMaterial = m; rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return rd;
            }
            float r = Mathf.Max(0.8f, sg.sr * 0.7f);
            Part(new Vector3(0f, 0.02f, 0f), new Vector3(2f * r + 0.3f, 0.04f, 2f * r + 0.3f), dark);
            Part(new Vector3(0f, 0.05f, 0f), new Vector3(2f * r, 0.04f, 2f * r), hazard);
            for (int k = -1; k <= 1; k += 2) Part(new Vector3(0f, 0.08f, k * r * 0.45f), new Vector3(2f * r, 0.02f, 0.18f), dark);
            Part(new Vector3(r + 0.4f, 0.5f, 0f), new Vector3(0.12f, 1.0f, 0.12f), steel);
            return Part(new Vector3(r + 0.4f, 1.1f, 0f), new Vector3(0.35f, 0.35f, 0.35f), lampRed);
        }

        // a racer at p: on a closed switch's plate? Then that gate opens for everyone, for the rest of the race
        public bool TryOpen(Vector3 p, string who, out SwitchGate opened)
        {
            opened = null;
            foreach (var g in Gates)
            {
                if (g.Open) continue;
                var d = p - new Vector3(g.Data.sx, g.Data.sy, g.Data.sz);
                float reach = g.Data.sr > 0f ? g.Data.sr : 1.6f;
                if (d.magnitude > reach + 0.8f) continue;   // the body's middle is up to 0.8 m off the plate's surface
                g.Open = true; g.OpenedAt = Time.time; g.OpenedBy = who; GateOpened?.Invoke(Gates.IndexOf(g));   // Round 57 (F)
                if (g.Block) g.Block.enabled = false;
                if (g.Lamp) g.Lamp.sharedMaterial = lampGreen;
                opened = g; return true;
            }
            return false;
        }

        // Round 57 (F): a race replay's gates: all shut (going back in time) or as they were (leaving the replay); one opened at its moment
        public static event System.Action<int> GateOpened;
        bool[] liveGates;
        public void ResetGatesForReplay(bool shut)
        {
            if (shut) { if (liveGates == null) liveGates = Gates.Select(g => g.Open).ToArray(); for (int i = 0; i < Gates.Count; i++) SetGate(i, false); }
            else if (liveGates != null) { for (int i = 0; i < Gates.Count && i < liveGates.Length; i++) SetGate(i, liveGates[i]); liveGates = null; }
        }
        public void OpenGateForReplay(int i) { if (i < 0 || i >= Gates.Count || Gates[i].Open) return; var g = Gates[i]; g.Open = true; g.OpenedAt = Time.time; if (g.Block) g.Block.enabled = false; if (g.Lamp) g.Lamp.sharedMaterial = lampGreen; }
        void SetGate(int i, bool open)
        {
            var g = Gates[i]; g.Open = open; g.OpenedAt = Time.time - 10f; if (g.Block) g.Block.enabled = !open; if (g.Lamp) g.Lamp.sharedMaterial = open ? lampGreen : lampRed;
            if (g.Grille) g.Grille.position = g.Closed + (open ? Vector3.up * (g.Data.gate.h + 3.0f) : Vector3.zero);
        }

        // a shortcut behind a switch gate is open only once its switch has been hit
        public bool ShortcutOpen(string shortcutId)
        {
            foreach (var g in Gates) if (g.Data.shortcut == shortcutId) return g.Open;
            return true;
        }

        void Update()
        {
            foreach (var g in Gates)
            {
                if (!g.Open || !g.Grille) continue;
                float t = Mathf.Clamp01((Time.time - g.OpenedAt) / 1.2f);
                // Round 57 (D): lifted well clear (it hung 0.4 m over a car's roof and read as a railing you could drive under)
                g.Grille.position = g.Closed + Vector3.up * (g.Data.gate.h + 3.0f) * Mathf.SmoothStep(0f, 1f, t);
            }
        }
    }
}
