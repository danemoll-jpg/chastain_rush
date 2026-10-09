using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 5: what the driver sees of the route. Everything here is scenery with no colliders, built at runtime from the
    // route so a direction change needs no rebuild.
    //   start / finish: a black-and-white chequered arch across the road, always standing (it is a landmark in free driving too);
    //   checkpoints: an arch each, shown only during a race. The NEXT one is bright green with two tall beams you can see
    //   over hills and trees from far down the road; later ones are amber arches; ones skipped this lap are red; passed ones are hidden.
    public sealed class RaceMarkers : MonoBehaviour
    {
        class Arch { public GameObject root; public Renderer[] paint; public GameObject beams; public RaceGate gate; }
        readonly List<Arch> arches = new List<Arch>();
        Arch finish;
        RaceController race;
        Material fallbackWhite, fallbackBlack, fallbackNext, fallbackLater, fallbackMissed;

        public void Build(RaceController r)
        {
            race = r;
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            arches.Clear();
            finish = MakeFinish(r.Route.Gate);
            foreach (var g in r.Route.Checkpoints) arches.Add(MakeCheckpoint(g));
        }

        public void Refresh(RaceController r)
        {
            bool racing = r.State != RaceController.Stage.Free;
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
        }

        // ------------------------------------------------------------ pieces

        Arch MakeCheckpoint(RaceGate g)
        {
            var root = Frame(g, "Checkpoint " + g.Id);
            var paint = new List<Renderer>();
            float h = 7.5f;
            paint.Add(Box(root.transform, new Vector3(-g.HalfWidth, h / 2f, 0), new Vector3(0.9f, h, 0.9f)));
            paint.Add(Box(root.transform, new Vector3(g.HalfWidth, h / 2f, 0), new Vector3(0.9f, h, 0.9f)));
            paint.Add(Box(root.transform, new Vector3(0, h - 0.6f, 0), new Vector3(g.HalfWidth * 2f, 1.2f, 0.5f)));
            var beams = new GameObject("Beams"); beams.transform.SetParent(root.transform, false);
            foreach (float side in new[] { -1f, 1f })
                paint.Add(Box(beams.transform, new Vector3(side * g.HalfWidth, 40f, 0), new Vector3(1.0f, 80f, 1.0f)));
            return new Arch { root = root, paint = paint.ToArray(), beams = beams, gate = g };
        }

        Arch MakeFinish(RaceGate g)
        {
            var root = Frame(g, "Start-finish gate");
            float h = 7.5f;
            foreach (float side in new[] { -1f, 1f })
            {
                // chequered pylons: alternating black and white blocks
                for (int k = 0; k < 6; k++)
                    Box(root.transform, new Vector3(side * g.HalfWidth, 0.6f + k * 1.2f, 0), new Vector3(1.0f, 1.2f, 1.0f), (k % 2 == 0) == (side > 0) ? Black : White);
            }
            int cells = Mathf.RoundToInt(g.HalfWidth * 2f / 1.5f);
            float cw = g.HalfWidth * 2f / cells;
            for (int c = 0; c < cells; c++)
                for (int row = 0; row < 2; row++)
                    Box(root.transform, new Vector3(-g.HalfWidth + cw * (c + 0.5f), h - 0.6f - row * 0.9f + 0.45f, 0), new Vector3(cw, 0.9f, 0.5f), ((c + row) % 2 == 0) ? Black : White);
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
        static Material Unlit(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var m = new Material(sh);
            m.SetColor("_BaseColor", c); m.color = c;
            return m;
        }
    }
}
