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

        // Round 79 (C, Dan: "the arrow is too jittery"): measured in the exe, the arrow placed in LateUpdate from the camera moved against the view at
        // 120 m/s (the chase camera may move after it in the frame) and rolled with the camera. Now it is placed as the camera renders (its final
        // pose that frame): always at the same spot on the screen; it points along a filtered direction to the ring (eased over 0.3 s, so a ring
        // passed swings it round smoothly) with the world's up (not the camera's or the plane's roll), its turn damped again over 0.12 s.
        // Old: Round 74's way (checks: -r79ArrowOld, the "before" numbers).
        static bool? old; public static bool Old => old ??= CheckRun.Active && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-r79ArrowOld") >= 0;
        public static Quaternion ShownOnScreen { get; private set; } = Quaternion.identity;   // checks: its turn as seen in the view
        public static Vector3 ShownAt { get; private set; }                                      // checks: where it is on the screen (pixels)
        public static bool Shown { get; private set; }
        Vector3 dirF; Quaternion rotF; bool fOn; float lastAt = -1f;

        void OnEnable() { RenderPipelineManager.beginCameraRendering += OnCam; }
        void OnDisable() { RenderPipelineManager.beginCameraRendering -= OnCam; Shown = false; }

        bool On(out RaceController race, out Camera cam)
        {
            race = RaceController.Instance; cam = Camera.main;
            return race != null && race.Route != null && race.Route.Air && cam != null && race.State != RaceController.Stage.Free && race.State != RaceController.Stage.Finished
                   && !TrailerMode.HudHidden && !ReplayViewer.Active && race.Target != null;
        }

        void LateUpdate()
        {
            if (!On(out var race, out var cam)) { if (arrow) arrow.gameObject.SetActive(false); steadyOn = steadyRotOn = fOn = false; Shown = false; return; }
            if (!arrow) Build();
            arrow.gameObject.SetActive(true);
            if (!Old) return;   // placed as the camera renders (OnCam)
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

        void OnCam(ScriptableRenderContext ctx, Camera cam)
        {
            if (!arrow || !arrow.gameObject.activeSelf || cam != Camera.main) return;
            if (Old) { Measure(cam); return; }   // (measured as it is drawn, as the new way is)
            if (!On(out var race, out _)) return;
            var ct = cam.transform; var at = ct.position + ct.forward * Ahead + ct.up * Up;
            float now = Time.unscaledTime, dt = lastAt < 0f ? 0f : Mathf.Clamp(now - lastAt, 0f, .1f);
            if (dt <= 0f && fOn) { arrow.SetPositionAndRotation(at, rotF); return; }   // (a second camera pass in the same frame)
            lastAt = now;
            var raw = race.Target.Position - ct.position; raw = raw.sqrMagnitude > 1f ? raw.normalized : ct.forward;
            if (!fOn) { dirF = raw; rotF = Look(dirF, ct.forward); fOn = true; }
            dirF = Vector3.Slerp(dirF, raw, 1f - Mathf.Exp(-dt / .3f)).normalized;
            rotF = Quaternion.Slerp(rotF, Look(dirF, ct.forward), 1f - Mathf.Exp(-dt / .12f));
            arrow.SetPositionAndRotation(at, rotF);
            Measure(cam);
        }
        static Quaternion Look(Vector3 d, Vector3 fallbackUp) => Mathf.Abs(Vector3.Dot(d, Vector3.up)) < .995f ? Quaternion.LookRotation(d, Vector3.up) : Quaternion.LookRotation(d, fallbackUp);
        void Measure(Camera cam) { ShownOnScreen = Quaternion.Inverse(cam.transform.rotation) * arrow.rotation; ShownAt = cam.WorldToScreenPoint(arrow.position); Shown = true; }

        void OnDestroy() { if (mat) Destroy(mat); }
    }
}
