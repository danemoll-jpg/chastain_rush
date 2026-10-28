using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 78 (E, Dan's BUG-003: "can we make the stoplights and cars not so blocky"): parked cars as the traffic cars' own models (Racer's traffic kit:
    // sedan, wagon, pickup, van; VehicleVisual.TrafficModel) in the paints traffic uses, at the spots the map build left (Marietta's bays, the parking decks;
    // MapBuilder.Kennestone.cs ParkedCar). Only those within Near of the camera are made (pooled); solid ones get a box (a parked car stops you as a car does).
    public sealed class ParkedCars : MonoBehaviour
    {
        public Vector3[] at; public float[] yaw; public int[] seed; public bool solid;
        const float Near = 320f, Far = 400f;
        static readonly string[] Models = { "TrafficSedan", "TrafficWagon", "TrafficPickup", "TrafficVan" };
        static Material[] paints;
        readonly Dictionary<int, GameObject> live = new();
        readonly Stack<GameObject>[] pool = { new(), new(), new(), new() };
        float next; Vector3 center; float radius;

        void Start()
        {
            if (at == null || at.Length == 0) { enabled = false; return; }
            var b = new Bounds(at[0], Vector3.zero); foreach (var p in at) b.Encapsulate(p); center = b.center; radius = b.extents.magnitude;
            if (paints == null)
            {
                paints = new Material[AmbientVehicle.Paints.Length];
                for (int i = 0; i < paints.Length; i++) paints[i] = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Parked paint " + i, color = AmbientVehicle.Paints[i], enableInstancing = true };
            }
        }

        void Update()
        {
            if (Time.time < next) return; next = Time.time + .4f;
            var cam = Camera.main; if (!cam) return; var eye = cam.transform.position;
            if ((eye - center).magnitude > radius + Far) { if (live.Count > 0) foreach (var k in new List<int>(live.Keys)) Drop(k); return; }
            for (int i = 0; i < at.Length; i++)
            {
                float d = (at[i] - eye).sqrMagnitude;
                if (d < Near * Near) { if (!live.ContainsKey(i)) live[i] = Make(i); }
                else if (d > Far * Far && live.ContainsKey(i)) Drop(i);
            }
        }

        int Kind(int i) { int v = (((seed[i] * 1103515245 + 12345) >> 8) & 0xFFFF) % 16; return v < 7 ? 0 : v < 11 ? 1 : v < 14 ? 2 : 3; }

        GameObject Make(int i)
        {
            int k = Kind(i); GameObject go;
            if (pool[k].Count > 0) go = pool[k].Pop();
            else
            {
                go = new GameObject("Parked " + Models[k]); go.transform.SetParent(transform, false);
                var painted = new List<Renderer>(); int pi = Mathf.Abs(seed[i] * 7 + 3) % paints.Length;
                VehicleVisual.TrafficModel(go.transform, Models[k], new List<Transform>(), painted, paints[pi]);
                if (solid) { var bc = go.AddComponent<BoxCollider>(); float h = k == 3 ? 1.72f : k == 2 ? 1.32f : 1.28f; bc.size = new Vector3(2.0f, h, 4.3f); bc.center = new Vector3(0f, (h - .55f) * .5f, 0f); }
            }
            go.transform.SetPositionAndRotation(at[i] + Vector3.up * .55f, Quaternion.Euler(0f, yaw[i], 0f));
            go.SetActive(true); return go;
        }

        void Drop(int i) { var go = live[i]; live.Remove(i); if (!go) return; go.SetActive(false); pool[Kind(i)].Push(go); }
    }
}
