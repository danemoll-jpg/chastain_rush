using System.Collections.Generic;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Development check only (added by Editor/Round7Check.cs, never saved into the scene). In Play mode it renders the Round 7
    // screenshots from about the viewpoints of the reference photos (one from the street for each house style, Dan's house, the
    // entrance island, the pond and fountain, a street with sidewalks) and then stages the geese: a car drives at the flock on the
    // road beside the pond, and the scatter is photographed. It also reports how the geese and the speed cost behaved.
    public sealed class Round7Runner : MonoBehaviour
    {
        [System.Serializable] public class ViewPt { public string label; public float x, z, fx, fz, qx, qz; public int index; }
        [System.Serializable] public class ViewsJson { public List<ViewPt> points = new List<ViewPt>(); }

        public string viewsJsonPath, outputDir;
        ViewsJson views;
        ArcadeVehicle car;
        Camera cam;
        readonly StringBuilder report = new StringBuilder();
        int frame, step;
        float t0;
        GooseFlock geese;

        ViewPt Get(string label) => views.points.Find(v => v.label == label);
        static Vector3 V3(float x, float y, float z) => new Vector3(x, y, z);
        float Ground(float x, float z) => Physics.Raycast(V3(x, 500f, z), Vector3.down, out var h, 1000f, 1, QueryTriggerInteraction.Ignore) ? h.point.y : 0f;

        void Start()
        {
            views = JsonUtility.FromJson<ViewsJson>(File.ReadAllText(viewsJsonPath));
            car = FindAnyObjectByType<ArcadeVehicle>();
            geese = FindAnyObjectByType<GooseFlock>();
            var main = Camera.main;
            var go = new GameObject("ShotCamera"); cam = go.AddComponent<Camera>();
            cam.fieldOfView = 62f; cam.nearClipPlane = main.nearClipPlane; cam.farClipPlane = main.farClipPlane; cam.tag = "Untagged";
            main.enabled = false;
            car.enabled = false;
            Directory.CreateDirectory(outputDir);
        }

        void Update()
        {
            frame++;
            if (frame < 150) return;   // let the terrain settle and the fountain spray build up
            if (step == 0) { var fs = FindAnyObjectByType<FountainSpray>(); report.AppendLine($"fountain: {(fs ? fs.Alive : -1)} spray particles alive"); StillShots(); step = 1; t0 = Time.time; PrepareChase(); return; }
            if (step == 1) Chase();
        }

        void Shot(string file, Vector3 pos, Vector3 target, string label, float fov = 62f)
        {
            cam.fieldOfView = fov;
            cam.transform.position = pos; cam.transform.rotation = Quaternion.LookRotation(target - pos, Vector3.up);
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outputDir, file), tex.EncodeToPNG());
            Destroy(tex); rt.Release();
            report.AppendLine($"{file}: {label}");
        }

        // a camera on the street in front of a house, about where the photos were taken from
        void HouseShot(string file, string key, string label, float fov = 66f)
        {
            var v = Get(key);
            if (v == null) { report.AppendLine($"{file}: no house for '{key}'"); return; }
            var q = new Vector2(v.qx, v.qz); var c = new Vector2(v.x, v.z);
            var toHouse = (c - q).normalized;
            var cp = q - toHouse * 3f;                     // a little behind the street centre, on the far side
            var pos = V3(cp.x, Ground(cp.x, cp.y) + 1.65f, cp.y);
            var target = V3(c.x, Ground(c.x, c.y) + 3.0f, c.y);
            Shot(file, pos, target, $"{label} (house {v.index}) from the street, {Vector2.Distance(cp, c):0} m away", fov);
        }

        void StillShots()
        {
            car.Body.position = V3(0f, -400f, 0f); car.transform.position = V3(0f, -400f, 0f); Physics.SyncTransforms();
            var isl = Get("island");
            if (isl != null)
            {
                var m = new Vector2(isl.x, isl.z); var ax = new Vector2(isl.fx, isl.fz).normalized;
                var left = new Vector2(-ax.y, ax.x);
                var cp = m - ax * 16f - left * 5.5f;
                Shot("01_entrance_island.png", V3(cp.x, Ground(cp.x, cp.y) + 1.6f, cp.y), V3(m.x + ax.x * 4f, Ground(m.x, m.y) + 2.8f, m.y + ax.y * 4f), "the entrance island: planted lens, trimmed shrubs, tall tree, the two signs on posts at its ends, the rail fence beyond", 68f);
                var cp2 = m + ax * 20f + left * 6f;
                Shot("01b_entrance_island_from_inside.png", V3(cp2.x, Ground(cp2.x, cp2.y) + 1.6f, cp2.y), V3(m.x, Ground(m.x, m.y) + 2.5f, m.y), "the island from the north end, looking back toward the entrance", 68f);
            }
            HouseShot("02_dan_house.png", "dan", "Dan's house");
            HouseShot("03_style_A_brick.png", "style_A_brick", "style A, brick front");
            HouseShot("04_style_A_siding.png", "style_A_siding", "style A, all siding");
            HouseShot("05_style_B_colonial.png", "style_B", "style B, brick colonial with columns");
            HouseShot("06_style_C_ranch.png", "style_C", "style C, one-storey ranch");
            HouseShot("07_style_D_plain.png", "style_D", "style D, plain two-storey");
            var d = Get("dan");
            if (d != null)
            {
                // Dan's house from a higher angle on the drive, and from the backyard hill
                var c = new Vector2(d.x, d.z); var f = new Vector2(d.fx, d.fz).normalized; var q = new Vector2(d.qx, d.qz);
                var p = c + f * 13f + new Vector2(f.y, -f.x) * 3f;
                Shot("02b_dan_house_drive.png", V3(p.x, Ground(p.x, p.y) + 1.5f, p.y), V3(c.x, Ground(c.x, c.y) + 3.2f, c.y), "Dan's house from the end of the drive (dark asphalt, large front-yard tree)", 72f);
                var back = c - f * 22f + new Vector2(f.y, -f.x) * 8f;
                var backTarget = c - f * 6f;
                Shot("02c_dan_house_backyard_hill.png", V3(back.x, Ground(back.x, back.y) + 2.6f, back.y), V3(backTarget.x, Ground(backTarget.x, backTarget.y) + 2.5f, backTarget.y), "behind Dan's house: the hill under pine straw, planted trees, the plain back", 70f);
            }
            var st = Get("style_A_siding");
            var fo = Get("fountain"); var gb = Get("geese_bank"); var gr = Get("geese_road");
            if (gb != null && fo != null)
            {
                var b = new Vector2(gb.x, gb.z); var fc = new Vector2(fo.x, fo.z);
                var pos = V3(b.x, Ground(b.x, b.y) + 1.6f, b.y);
                Shot("08_pond_fountain.png", pos, V3(fc.x, Ground(fc.x, fc.y) + 3.2f, fc.y), "the pond with the fountain from the bank by the street (weeping trees, retaining walls and docks on the far shore)", 58f);
                var near = fc - (fc - b).normalized * 24f;
                Shot("08c_fountain_close.png", V3(near.x, Mathf.Max(Ground(near.x, near.y), FindAnyObjectByType<FountainSpray>().transform.position.y) + 1.8f, near.y), V3(fc.x, Ground(fc.x, fc.y) + 3.5f, fc.y), "the fountain from the shore, about 24 m away", 60f);
                Shot("08b_pond_wide.png", pos + V3(0f, 2.2f, 0f), V3(fc.x + (fc.x - b.x) * 0.2f, Ground(fc.x, fc.y) + 1f, fc.y + (fc.y - b.y) * 0.2f), "the pond, wide", 78f);
            }
            if (st != null)
            {
                var c = new Vector2(st.x, st.z); var q = new Vector2(st.qx, st.qz);
                var along = new Vector2(-(c - q).normalized.y, (c - q).normalized.x);
                var cp = q - along * 14f;
                Shot("09_street_sidewalks.png", V3(cp.x, Ground(cp.x, cp.y) + 1.7f, cp.y), V3(q.x + along.x * 18f, Ground(q.x, q.y) + 1.2f, q.y + along.y * 18f), "a street with curbs, grass strips, sidewalks, mailboxes and lamps, looking along it", 64f);
            }
        }

        // ---- the geese: a car drives at the flock on the road beside the pond and the scatter is photographed
        Vector2 roadC, dirC, camSide;
        float carT, carDist = 46f, carSpeed = 11f;
        int chaseShot; float scatterAt = -1f;
        bool chaseReady;

        void PrepareChase()
        {
            var gr = Get("geese_road"); var fo = Get("fountain");
            if (gr == null || geese == null) return;
            roadC = new Vector2(gr.x, gr.z);
            // the road direction here: toward the nearest road point further along, taken from the car's path across the zone
            var toPond = (new Vector2(fo.x, fo.z) - roadC).normalized;
            dirC = new Vector2(-toPond.y, toPond.x);      // along the road (perpendicular to the way to the pond)
            camSide = toPond;
            chaseReady = true;
            carT = 0f;
            var start = roadC - dirC * carDist;
            car.Body.position = V3(start.x, Ground(start.x, start.y) + 0.8f, start.y);
            report.AppendLine($"chase: geese zone on the road at ({roadC.x:0},{roadC.y:0}); car drives at it from {carDist:0} m at {carSpeed:0} m/s");
        }

        void Chase()
        {
            if (!chaseReady || chaseShot >= 5) { Finish(); return; }
            float dt = Time.deltaTime;
            carT += dt;
            var pos2 = roadC - dirC * (carDist - carSpeed * carT);
            var pos = V3(pos2.x, Ground(pos2.x, pos2.y) + 0.8f, pos2.y);
            var rot = Quaternion.LookRotation(V3(dirC.x, 0f, dirC.y), Vector3.up);
            car.Body.position = pos; car.Body.rotation = rot; car.transform.SetPositionAndRotation(pos, rot);
            car.Body.linearVelocity = V3(dirC.x, 0f, dirC.y) * carSpeed;
            // the camera stands on the pond side of the road, a little back, looking at the flock
            var cp = roadC + camSide * 9f - dirC * 10f;
            var camPos = V3(cp.x, Ground(cp.x, cp.y) + 1.5f, cp.y);
            var look = V3(roadC.x, Ground(roadC.x, roadC.y) + 0.8f, roadC.y);
            float dist = Vector2.Distance(pos2, roadC);
            if (chaseShot == 0 && carT > 0.5f) { Shot("10_geese_calm.png", camPos, look, "the geese before the car comes (on the bank, on the road, on the water)", 60f); chaseShot = 1; }
            if (chaseShot >= 1 && scatterAt < 0f && geese.ScatteredCount > 0) { scatterAt = carT; report.AppendLine($"chase: first goose scattered at t = {carT:0.00} s, car {dist:0.0} m from the zone centre"); }
            if (scatterAt >= 0f)
            {
                float[] times = { 0.15f, 0.65f, 1.3f };
                if (chaseShot - 1 < times.Length && carT - scatterAt >= times[chaseShot - 1])
                {
                    Shot($"10_geese_scatter_{chaseShot}.png", camPos, look + V3(0f, 1.2f, 0f), $"geese scattering {carT - scatterAt:0.00} s after the first one went ({geese.ScatteredCount} of {geese.BirdCount} up, {geese.FlyingCount} in the air)", 60f);
                    chaseShot++;
                }
                if (carT - scatterAt > 5f) { chaseShot = 99; }
            }
            if (carT > 9f) chaseShot = 99;
        }

        void Finish()
        {
            enabled = false;
            report.AppendLine($"geese after the chase: {geese.ScatteredCount} of {geese.BirdCount} away or flying, speed lost to {geese.HitsCount} goose hits {geese.TotalSpeedLost * 100f:0.0}%");
            File.WriteAllText(Path.Combine(outputDir, "round7_shots.txt"), report.ToString());
            Debug.Log("[Round7Check]\n" + report);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
