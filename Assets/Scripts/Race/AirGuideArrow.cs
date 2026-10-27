using UnityEngine;
using UnityEngine.Rendering;

namespace ChastainRush
{
    // Round 73 (B2, Dan on the Quarry Air Race: "there was an arrow but it was only pointing up or down, turning the plane around I couldn't find it at
    // all"): a 3D arrow floating ahead of the player's view, pointing straight at the ring the guide wants next, in full direction (left, right, up,
    // down, behind). The flat HUD arrow turned only by heading (behind you it pointed down the screen); this one is a real arrow in the world, so it
    // points through the plane's own bank and pitch too. Green at the ring the guide points at; the HUD under it names the ring and its distance.
    // Only in an air race while it runs (RaceController.Target: past a missed ring, the next one).
    public sealed class AirGuideArrow : MonoBehaviour
    {
        public const float Ahead = 15f, Up = 3.6f;   // where it floats: this far ahead of the camera, this far up the screen
        Transform arrow; Material mat;

        void Build()
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color")) { name = "Air guide arrow" };
            var col = new Color(.35f, 1f, .25f); mat.SetColor("_BaseColor", col); mat.color = col;
            if (mat.HasProperty("_ZTest")) mat.SetFloat("_ZTest", (float)CompareFunction.Always);   // never hidden behind the plane or the ground
            mat.renderQueue = 4000;
            arrow = new GameObject("Air guide arrow").transform; arrow.SetParent(transform, false);
            var mesh = new Mesh { name = "Air guide arrow" };
            // along +z: a square shaft 0.5 x 0.5 from z -1.8 to 0.4, a four-sided head 1.7 wide from z 0.4 to 2.2
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 }); }
            void Tri(Vector3 a, Vector3 b, Vector3 c) { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1 }); }
            float w = .25f, z0 = -1.8f, z1 = .4f, hw = .85f, z2 = 2.2f;
            Quad(new Vector3(-w, -w, z0), new Vector3(w, -w, z0), new Vector3(w, -w, z1), new Vector3(-w, -w, z1));
            Quad(new Vector3(-w, w, z0), new Vector3(w, w, z0), new Vector3(w, w, z1), new Vector3(-w, w, z1));
            Quad(new Vector3(-w, -w, z0), new Vector3(-w, w, z0), new Vector3(-w, w, z1), new Vector3(-w, -w, z1));
            Quad(new Vector3(w, -w, z0), new Vector3(w, w, z0), new Vector3(w, w, z1), new Vector3(w, -w, z1));
            Quad(new Vector3(-w, -w, z0), new Vector3(w, -w, z0), new Vector3(w, w, z0), new Vector3(-w, w, z0));
            var tip = new Vector3(0, 0, z2); var c0 = new Vector3(-hw, -hw, z1); var c1 = new Vector3(hw, -hw, z1); var c2 = new Vector3(hw, hw, z1); var c3 = new Vector3(-hw, hw, z1);
            Tri(c0, c1, tip); Tri(c1, c2, tip); Tri(c2, c3, tip); Tri(c3, c0, tip); Quad(c0, c1, c2, c3);
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            arrow.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = arrow.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        }

        void LateUpdate()
        {
            var race = RaceController.Instance; var cam = Camera.main;
            bool on = race != null && race.Route != null && race.Route.Air && cam != null && race.State != RaceController.Stage.Free && race.State != RaceController.Stage.Finished
                      && !TrailerMode.HudHidden && !ReplayViewer.Active && race.Target != null;
            if (!on) { if (arrow) arrow.gameObject.SetActive(false); return; }
            if (!arrow) Build();
            arrow.gameObject.SetActive(true);
            var at = cam.transform.position + cam.transform.forward * Ahead + cam.transform.up * Up;
            var to = race.Target.Position - at;
            arrow.SetPositionAndRotation(at, Quaternion.LookRotation(to.sqrMagnitude > 1f ? to.normalized : cam.transform.forward, cam.transform.up));
        }

        void OnDestroy() { if (mat) Destroy(mat); }
    }
}
