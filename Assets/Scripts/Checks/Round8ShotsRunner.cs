using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Development check only (added by Editor/Round8Check.cs, never saved into the scene). Renders the Round 8 screenshots at
    // 1920 x 1080 from a separate camera: still views of both shortcuts (with a race started, so the blue entry arches and the
    // turn-off signs show), Dan's back yard, and the two jumps in flight (the real car driven along the line by
    // ArcadeVehicle.Simulate, photographed from the side when it is over house 1641 / between the two houses of #2).
    public sealed class Round8ShotsRunner : MonoBehaviour
    {
        [Serializable] class Sc { public string id; public float[] path; public Jump[] jumps; public Open[] open_houses; }
        [Serializable] class Jump { public string label; public float x, z; }
        [Serializable] class Open { public int building; public float[] a, b; public float floor_y; }
        [Serializable] class ScFile { public Sc[] shortcuts; }
        [Serializable] class ViewPt { public string label; public float x, z, fx, fz, qx, qz; }
        [Serializable] class Views { public List<ViewPt> points = new List<ViewPt>(); }

        public string outputDir, shortcutsJson, viewsJson;
        Camera cam; ArcadeVehicle car; RaceController race;
        readonly StringBuilder log = new StringBuilder();

        static float G(float x, float z) { float y = GroundQuery.Height(x, z); return float.IsNaN(y) ? 0f : y; }
        static Vector3 P(Vector2 p, float up) => new Vector3(p.x, G(p.x, p.y) + up, p.y);
        static Vector2 V(float[] a, int i = 0) => new Vector2(a[i], a[i + 1]);

        IEnumerator Start()
        {
            var main = Camera.main;
            cam = new GameObject("ShotCamera").AddComponent<Camera>();
            cam.nearClipPlane = main.nearClipPlane; cam.farClipPlane = main.farClipPlane;
            main.enabled = false;
            car = FindAnyObjectByType<ArcadeVehicle>(); race = RaceController.Instance;
            for (int i = 0; i < 120; i++) yield return null;
            var data = JsonUtility.FromJson<ScFile>(shortcutsJson);
            var views = JsonUtility.FromJson<Views>(viewsJson);
            race.StartRace();                     // the race markers (blue arches, turn-off signs) show from the countdown on
            car.enabled = false;
            yield return null;
            Sc s2 = Array.Find(data.shortcuts, s => s.id.Contains("two_houses")), s6 = Array.Find(data.shortcuts, s => s.id.Contains("over_house"));
            var p2 = Pts(s2.path); var p6 = Pts(s6.path);

            // ---- #2
            var h1 = s2.open_houses[0]; var h2 = s2.open_houses[1];
            Vector2 a1 = V(h1.a), b1 = V(h1.b), a2 = V(h2.a), b2 = V(h2.b), u = (b1 - a1).normalized;
            Shot("01_sc2_turnoff_from_lane", P(p2[0] - (p2[3] - p2[0]).normalized * 22f, 2.2f), P(a1, 3f), "#2 turn-off: the sign, the blue entry arch at house 1763's door, the drive-up ramp");
            Shot("02_sc2_house_1763_opening", new Vector3(a1.x, h1.floor_y + 1.6f, a1.y) - new Vector3(u.x, 0, u.y) * 12f, new Vector3(b1.x, h1.floor_y + 1.2f, b1.y), "#2: the passage through house 1763");
            Shot("03_sc2_inside_1763", new Vector3(a1.x, h1.floor_y + 1.4f, a1.y) + new Vector3(u.x, 0, u.y) * 3f, new Vector3(b2.x, h2.floor_y + 2f, b2.y), "#2: inside house 1763, the lip and house 1627 ahead");
            var lip = new Vector2(s2.jumps[0].x, s2.jumps[0].z);
            Shot("04_sc2_lip_landing_and_1627", P(lip, 2.4f) - new Vector3(u.x, 0, u.y) * 2f, new Vector3(a2.x, h2.floor_y + 1.5f, a2.y), "#2: off the lip, the landing ramp runs into house 1627's door");
            var side = new Vector2(-u.y, u.x);
            Shot("05_sc2_drop_side_view", P((lip + a2) * 0.5f + side * 34f, 7f), P((lip + a2) * 0.5f, 3f), "#2: the drop between the two houses from the side", 55f);
            Shot("06_sc2_gap_1615_1459", P(b2 + u * 4f, 1.8f), P(V(s2.path, s2.path.Length - 2 * (p2.Count / 4)), 1f), "#2: out of 1627 toward the gap between 1615 and 1459");

            // ---- #6
            var kick = new Vector2(s6.jumps[0].x, s6.jumps[0].z);
            int k = Nearest(p6, kick);
            Vector2 u6 = (p6[Mathf.Min(k + 3, p6.Count - 1)] - p6[k]).normalized, side6 = new Vector2(-u6.y, u6.x);
            Shot("07_sc6_turnoff_from_lane", P(p6[0] - (p6[2] - p6[0]).normalized * 25f, 2.2f), P(kick, 4f), "#6 turn-off: the kicker over house 1641 from the lane");
            Shot("08_sc6_side_view", P(kick + u6 * 12f + side6 * 40f, 20f), P(kick + u6 * 14f, 5f), "#6: kicker, house 1641 and the landing deck from the side", 60f);
            Shot("09_sc6_deck_looking_back", P(kick + u6 * 70f, 3f), P(kick + u6 * 10f, 6f), "#6: the landing deck behind the house, looking back at the kicker");

            // ---- Dan's back yard
            var dan = views.points.Find(v => v.label == "dan");
            if (dan != null)
            {
                Vector2 c = new Vector2(dan.x, dan.z), f = new Vector2(dan.fx, dan.fz).normalized;
                Shot("10_dan_backyard", P(c - f * 30f, 6f), P(c - f * 4f, 2f), "Dan's back yard: no fence on his lot; the neighbour on the right and the lot up the hill are fenced", 64f);
                Shot("11_dan_backyard_high", P(c - f * 22f + new Vector2(-f.y, f.x) * 10f, 18f), P(c - f * 14f, 0f), "Dan's back yard from above", 64f);
            }
            race.EndRace();

            // ---- the jumps in flight
            yield return Flight(p6, 24f, kick + u6 * 13f, P(kick + u6 * 12f + side6 * 36f, 17f), "12_sc6_in_flight_over_house");
            yield return Flight(p2, 24f, (lip + a2) * 0.5f, P((lip + a2) * 0.5f + side * 30f, 6f), "13_sc2_in_flight");
            File.WriteAllText(Path.Combine(outputDir, "screenshots.txt"), log.ToString());
            Debug.Log("[Round8Shots]\n" + log);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        static List<Vector2> Pts(float[] a) { var l = new List<Vector2>(); for (int i = 0; i + 1 < a.Length; i += 2) l.Add(new Vector2(a[i], a[i + 1])); return l; }
        static int Nearest(List<Vector2> p, Vector2 q) { int b = 0; for (int i = 1; i < p.Count; i++) if ((p[i] - q).sqrMagnitude < (p[b] - q).sqrMagnitude) b = i; return b; }

        void Shot(string file, Vector3 pos, Vector3 target, string label, float fov = 62f)
        {
            cam.fieldOfView = fov;
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(target - pos, Vector3.up));
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outputDir, file + ".png"), tex.EncodeToPNG());
            Destroy(tex); rt.Release(); Destroy(rt);
            log.AppendLine($"{file}.png  {label}  (camera {pos.x:0},{pos.y:0},{pos.z:0})");
        }

        // drives the car along a shortcut's line from its start at 'speed' and photographs it from 'camPos' when it passes 'at'
        IEnumerator Flight(List<Vector2> path, float speed, Vector2 at, Vector3 camPos, string file)
        {
            var respawn = car.GetComponent<VehicleRespawn>();
            var dir = (path[2] - path[0]).normalized;
            respawn.PlaceAt(P(path[0], 0.3f), Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg);
            yield return new WaitForFixedUpdate();
            car.Body.linearVelocity = new Vector3(dir.x, 0, dir.y) * speed;
            int next = 0; bool shot = false; float best = float.MaxValue;
            for (int step = 0; step < 50 * 20 && !shot; step++)
            {
                yield return new WaitForFixedUpdate();
                var pos = car.transform.position; var p2 = new Vector2(pos.x, pos.z);
                while (next < path.Count - 1 && (path[next] - p2).magnitude < 7f) next++;
                var to = path[next] - p2;
                float ang = Vector2.SignedAngle(to, new Vector2(car.transform.forward.x, car.transform.forward.z));
                car.Simulate(car.ForwardSpeed < speed ? 1f : 0f, 0f, Mathf.Clamp(ang / 20f, -1f, 1f), Time.fixedDeltaTime);
                float d = (p2 - at).magnitude;
                if (d < best) best = d;
                if (d < 2.5f || (best < 8f && d > best + 0.5f))
                {
                    Shot(file, camPos, pos, $"in flight at {speed:0} m/s: {pos.y - G(pos.x, pos.z):0.0} m above the ground, {car.Body.linearVelocity.magnitude:0} m/s");
                    shot = true;
                }
            }
            if (!shot) log.AppendLine($"{file}: the car did not pass the point (closest {best:0.0} m)");
        }
    }
}
