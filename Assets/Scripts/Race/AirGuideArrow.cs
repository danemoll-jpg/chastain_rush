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
        public const float Ahead = 14f, Up = -4.2f;  // where it floats: this far ahead of the camera, this far below the middle of the view (seen from a little
                                                     // above, so it reads as an arrow even when the ring is straight ahead: the check's first picture showed its back)
        Transform arrow; Material mat;
        Vector3 steadyFwd, steadyUp; Quaternion steadyRot; bool steadyOn, steadyRotOn;   // Round 74 (C)

        void Build()
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color")) { name = "Air guide arrow" };
            var col = new Color(.35f, 1f, .25f); mat.SetColor("_BaseColor", col); mat.color = col;
            if (mat.HasProperty("_ZTest")) mat.SetFloat("_ZTest", (float)CompareFunction.Always);   // never hidden behind the plane or the ground
            mat.renderQueue = 4000;
            arrow = new GameObject("Air guide arrow").transform; arrow.SetParent(transform, false);
            var mesh = new Mesh { name = "Air guide arrow" };
            // along +z, lying flat (its top up): a broad chevron 3.6 m wide and 4.4 m long, 0.35 m thick, its notch at the back
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            var outline = new[] { new Vector3(0f, 0f, 2.4f), new Vector3(1.8f, 0f, -1.4f), new Vector3(0f, 0f, -.4f), new Vector3(-1.8f, 0f, -1.4f) };
            const float th = .35f;
            void Tri(Vector3 a, Vector3 b, Vector3 c) { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1 }); }
            foreach (float y in new[] { 0f, -th }) { var up = Vector3.up * y; Tri(outline[0] + up, outline[1] + up, outline[2] + up); Tri(outline[0] + up, outline[2] + up, outline[3] + up); }
            for (int k = 0; k < 4; k++) { var a = outline[k]; var b2 = outline[(k + 1) % 4]; Tri(a, b2, b2 - Vector3.up * th); Tri(a, b2 - Vector3.up * th, a - Vector3.up * th); }
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            arrow.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = arrow.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        }

        void LateUpdate()
        {
            var race = RaceController.Instance; var cam = Camera.main;
            bool on = race != null && race.Route != null && race.Route.Air && cam != null && race.State != RaceController.Stage.Free && race.State != RaceController.Stage.Finished
                      && !TrailerMode.HudHidden && !ReplayViewer.Active && race.Target != null;
            if (!on) { if (arrow) arrow.gameObject.SetActive(false); steadyOn = steadyRotOn = false; return; }
            if (!arrow) Build();
            arrow.gameObject.SetActive(true);
            // Round 74 (C, Dan: "Arrows during vehicle races are too bouncy. Can we make them more stable"): placed from a steadied view (the camera's
            // forward and up eased over about a quarter second, so the camera's shake and bounce do not move it) and turned toward the ring with damping;
            // still pointing where the ring is
            float dt = Time.unscaledDeltaTime, k = 1f - Mathf.Exp(-dt / .25f);
            if (!steadyOn) { steadyFwd = cam.transform.forward; steadyUp = cam.transform.up; steadyOn = true; }
            steadyFwd = Vector3.Slerp(steadyFwd, cam.transform.forward, k).normalized; steadyUp = Vector3.Slerp(steadyUp, cam.transform.up, k).normalized;
            var at = cam.transform.position + steadyFwd * Ahead + steadyUp * Up;
            var to = race.Target.Position - at;
            var want = Quaternion.LookRotation(to.sqrMagnitude > 1f ? to.normalized : steadyFwd, Vector3.Slerp(steadyUp, Vector3.up, .5f));
            steadyRot = arrow.gameObject.activeSelf && steadyRotOn ? Quaternion.Slerp(steadyRot, want, 1f - Mathf.Exp(-dt / .15f)) : want; steadyRotOn = true;
            arrow.SetPositionAndRotation(at, steadyRot);
        }

        void OnDestroy() { if (mat) Destroy(mat); }
    }
}
