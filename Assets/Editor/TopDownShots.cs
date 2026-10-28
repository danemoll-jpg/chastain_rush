using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 11: straight-down orthographic pictures of the built scene (Assets/Generated/ChastainPrototype.unity), for looking
    // at roads, sidewalks, curbs and drives from above. Batch, with graphics (not -nographics):
    //   Unity.exe -batchmode -quit -projectPath . -executeMethod ChastainRush.TopDownShots.Run -topDownOut <dir> -topDownRects "name:x0:z0:x1:z1;..." [-topDownPx 2000]
    //   [-viewShots "name:x:y:z:tx:ty:tz;..."] (Round 39: perspective views)
    // Trees are drawn at runtime only, so they are not in these pictures (which is what is wanted here).
    public static class TopDownShots
    {
        public static void Run()
        {
            string outDir = Arg("-topDownOut") ?? "Temp/topdown";
            string rects = Arg("-topDownRects") ?? "";
            int px = int.Parse(Arg("-topDownPx") ?? "2000", CultureInfo.InvariantCulture);
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(MapBuilder.ScenePath, OpenSceneMode.Single);
            if (Arg("-r77Probe") != null) MapBuilder.R77ProbeOnScene();   // Round 77: the embankment pass on the scene's ground in memory, before the pictures
            if (Arg("-listAt") != null)   // Round 77: "x,z,r": every renderer whose bounds reach within r m of (x, z) and less than 60 m across (what a small thing is)
            {
                var v = Array.ConvertAll(Arg("-listAt").Split(','), F); var c = new Vector3(v[0], 0f, v[1]);
                foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var b = r.bounds; var q = b.ClosestPoint(new Vector3(c.x, b.center.y, c.z)); if (new Vector2(q.x - c.x, q.z - c.z).magnitude > v[2]) continue;
                    Debug.Log($"[ListAt] {r.name} (parent {(r.transform.parent ? r.transform.parent.name : "-")}) bounds {b.center} size {b.size} material {(r.sharedMaterial ? r.sharedMaterial.name : "-")}");
                }
            }
            if (Arg("-pickRay") != null)   // Round 77: "x,y,z,tx,ty,tz": which drawn mesh a ray from (x, y, z) toward (tx, ty, tz) meets first (temporary mesh colliders)
            {
                var v = Array.ConvertAll(Arg("-pickRay").Split(','), F); var ray = new Ray(new Vector3(v[0], v[1], v[2]), (new Vector3(v[3], v[4], v[5]) - new Vector3(v[0], v[1], v[2])).normalized);
                var temp = new System.Collections.Generic.List<Component>();
                foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (!mr.bounds.IntersectRay(ray, out float dist) || dist > 300f) continue; var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                    var go2 = new GameObject("pick " + mr.name); go2.transform.SetPositionAndRotation(mr.transform.position, mr.transform.rotation); go2.transform.localScale = mr.transform.lossyScale;
                    var mc = go2.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc); go2.layer = 31;
                }
                Physics.SyncTransforms();
                foreach (var h in Physics.RaycastAll(ray, 300f, 1 << 31).OrderBy(h => h.distance).Take(5)) Debug.Log($"[PickRay] {h.collider.name} at {h.point} ({h.distance:0.0} m)");
            }
            if (Arg("-hideTerrain") != null) foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)) t.drawHeightmap = false;   // Round 77: what is not ground
            RenderSettings.fog = false;   // the camera is hundreds of metres up
            var go = new GameObject("TopDownCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true; cam.nearClipPlane = 1f; cam.farClipPlane = 2000f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.magenta;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // looking down, +z (north) up in the picture
            var warm = new RenderTexture(64, 64, 24); cam.targetTexture = warm; cam.Render(); cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(warm);   // the first render comes out black
            foreach (var item in rects.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var f = item.Split(':');
                float x0 = F(f[1]), z0 = F(f[2]), x1 = F(f[3]), z1 = F(f[4]);
                float w = x1 - x0, d = z1 - z0;
                int pw = px, ph = Mathf.RoundToInt(px * d / w);
                go.transform.position = new Vector3((x0 + x1) / 2f, 900f, (z0 + z1) / 2f);
                cam.orthographicSize = d / 2f; cam.aspect = w / d;
                var rt = new RenderTexture(pw, ph, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt; cam.Render(); cam.Render();   // twice: a first render can come out unlit
                RenderTexture.active = rt;
                var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(outDir, f[0] + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
                Debug.Log($"[TopDown] {f[0]}: x {x0}..{x1}, z {z0}..{z1}, {pw} x {ph} px");
            }
            // Round 39: perspective views, "-viewShots name:x:y:z:tx:ty:tz;..." (from x y z, looking at tx ty tz; 60 degrees, 1600 x 900)
            cam.orthographic = false; cam.fieldOfView = 60f; cam.nearClipPlane = .1f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f);
            foreach (var item in (Arg("-viewShots") ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var f = item.Split(':'); var from = new Vector3(F(f[1]), F(f[2]), F(f[3])); var at = new Vector3(F(f[4]), F(f[5]), F(f[6]));
                go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from)); cam.aspect = 16f / 9f;
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(outDir, f[0] + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < a.Length; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
