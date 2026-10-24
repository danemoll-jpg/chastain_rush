using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 63 (4, Dan: "from the air much of the 129 km2 is in view at once ... in the helicopter (and at height in general) the fog pulls back to show
    // the map out to several km"): the view opens with the camera's height over the ground. On the ground nothing changes (the conditions' fog,
    // 650 m by day, the 750 m far clip). From 30 m up it opens, by 450 m up it is ten times as far (6.5 km by day; the night's, the rain's and the
    // snow's fog open the same way from their own distances), the far clip with it; FarTerrain draws the terrain tiles to the far clip.
    // What would cost too much that far: the small things the map build merged into 128 m cells (road paint, drives, house and business detail,
    // signs, fences, mailboxes, hydrants, lamps, the network under the ground) are not drawn past 900 m (a CullingGroup over their bounds; their
    // colliders stay; on the ground the far clip is nearer than that, so the ground view is as before); houses, business buildings, roads and lots
    // are drawn as far as the view. The trees: TreeField draws past its own 700 m out to the view with an eight-sided shape per tree (TreeField.Far).
    // World, not game layer; made on its own when a scene with terrain loads.
    [DefaultExecutionOrder(1000)]
    public sealed class FarView : MonoBehaviour
    {
        public const float OpenFrom = 30f, OpenBy = 450f, Opens = 10f, MaxView = 6500f, DetailFar = 900f, TreeFar = 5000f;   // past 5 km the fog all but hides a tree
        public static float View { get; private set; } = 650f;   // the fog's end now
        public static float Height { get; private set; }         // the main camera over the ground
        // Round 66 (the map's edges): the view also opens by the camera's height over the land's low point round it (EdgeScenery.RegionalLow), so it
        // opens on the summit; while it is open the far clip goes out to EdgeScenery.HorizonFar for the horizon's layer only, every other layer
        // keeping the world's own far clip (WorldFar; FarTerrain and the edge scenery's trees use it)
        public static float Lift { get; private set; }
        public static bool Opened { get; private set; }
        public static float WorldFar { get; private set; } = 750f;
        readonly float[] cullDistances = new float[32]; bool culling;
        float baseStart = 100f, baseEnd = 650f, baseFar = 750f; Camera cam; bool haveBase;
        CullingGroup details; Renderer[] renderers; BoundingSphere[] spheres; Camera detailCam;
        public static int DetailCount { get; private set; }
        // checks only (Round20Runner.R63 hfpsx): caps on the opened view's far clip and on the far trees' distance (0 = none)
        public static float ExpClip, ExpTrees;
        public static int DetailHidden { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<FarView>() || Terrain.activeTerrains.Length == 0) return;
            new GameObject("Far view").AddComponent<FarView>();
        }

        static readonly string[] DetailNames =
        {
            "RoadPaint", "BusinessDetail", "MoreHydrants", "BusinessSigns", "Driveways", "HouseDetail", "MailboxesRest", "Fence_", "BusinessLotLamps",
            "BusinessLotPaint", "MallSign", "StoreSign", "HoodHouseDetail", "HoodProps", "Mailboxes", "HoodSidewalks", "StreetLamps", "MallLotLamps", "MallLotPaint",
            "Hydrants", "Noonday trail center line", "Shrub beds", "Airport paint", "Winding road sign", "Crypt", "Net ", "StuntPark", "Enclave", "sc ", "Helipad paint",
        };
        static bool IsDetail(Renderer r)
        {
            string n = r.gameObject.name;
            foreach (var d in DetailNames) if (n.StartsWith(d)) return true;
            if (!r.gameObject.isStatic) return false;
            var s = r.bounds.size; return Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < 40f;   // a small fixed thing of its own
        }

        System.Collections.IEnumerator Start()
        {
            yield return null; yield return null;   // after the scene's own start-up
            var list = new List<Renderer>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if (!r || r.GetComponentInParent<LODGroup>() || r.GetComponentInParent<Racer.ArcadeVehicle>() || r.gameObject.scene != gameObject.scene) continue;
                if (IsDetail(r)) list.Add(r);
            }
            renderers = list.ToArray(); spheres = new BoundingSphere[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) { var b = renderers[i].bounds; spheres[i] = new BoundingSphere(b.center, b.extents.magnitude); }
            details = new CullingGroup(); details.SetBoundingSpheres(spheres); details.SetBoundingSphereCount(spheres.Length);
            details.SetBoundingDistances(new[] { DetailFar }); details.onStateChanged = Changed;
            DetailCount = renderers.Length;
            Debug.Log($"[Round63] far view: {renderers.Length} detail renderers not drawn past {DetailFar:0} m");
        }
        void Changed(CullingGroupEvent e)
        {
            if (e.index >= renderers.Length || !renderers[e.index]) return;
            bool off = e.currentDistance > 0; if (renderers[e.index].forceRenderingOff == off) return;
            renderers[e.index].forceRenderingOff = off; DetailHidden += off ? 1 : -1;
        }
        void OnDestroy() { details?.Dispose(); details = null; }

        void LateUpdate()
        {
            if (!cam || !cam.isActiveAndEnabled) { cam = Camera.main; if (!cam) return; baseFar = Mathf.Max(baseFar, cam.farClipPlane); }
            if (details != null && detailCam != cam) { details.targetCamera = cam; details.SetDistanceReferencePoint(cam.transform); detailCam = cam; }
            // the conditions' own fog (WorldLook's preset), or the scene's as it started
            var look = Racer.WorldLook.Current; var p = look ? look.Preset : null;
            if (p != null) { baseStart = p.fogStart; baseEnd = p.fogEnd; haveBase = true; }
            else if (!haveBase) { baseStart = RenderSettings.fogStartDistance; baseEnd = RenderSettings.fogEndDistance; haveBase = true; }
            var at = cam.transform.position;
            Height = GroundQuery.TryHeight(at.x, at.z, out float g) ? at.y - g : 0f;
            Lift = EdgeScenery.RegionalLow(at.x, at.z, out float low) ? Mathf.Max(Height, at.y - low) : Height;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(OpenFrom, OpenBy, Lift));
            if (SplitScreen.Active || cam.orthographic) k = 0f;
            float end = Mathf.Min(Mathf.Lerp(baseEnd, baseEnd * Opens, k), Mathf.Max(baseEnd, MaxView)), start = Mathf.Lerp(baseStart, end * .45f, k);
            View = end;
            if (!Mathf.Approximately(RenderSettings.fogEndDistance, end)) RenderSettings.fogEndDistance = end;
            if (!Mathf.Approximately(RenderSettings.fogStartDistance, start)) RenderSettings.fogStartDistance = start;
            Opened = k > 0f;
            if (!SplitScreen.Active)
            {
                float far = k > 0f ? Mathf.Max(baseFar, end + 100f) : baseFar; if (ExpClip > 0f) far = Mathf.Min(far, ExpClip);
                WorldFar = far;
                bool horizon = k > 0f && EdgeScenery.Instance;
                if (horizon)
                {
                    for (int i = 0; i < 32; i++) cullDistances[i] = i == EdgeScenery.HorizonLayer ? 0f : far;
                    cam.layerCullDistances = cullDistances; cam.layerCullSpherical = true; culling = true; far = EdgeScenery.HorizonFar;
                }
                else if (culling) { cam.layerCullDistances = new float[32]; culling = false; }
                if (!Mathf.Approximately(cam.farClipPlane, far)) cam.farClipPlane = far;
            }
            else { WorldFar = cam.farClipPlane; if (culling) { cam.layerCullDistances = new float[32]; culling = false; } }
            TreeField.FarDraw = k > 0f ? Mathf.Min(ExpTrees > 0f ? Mathf.Min(end, ExpTrees) : end, TreeFar) : 0f;
        }
    }
}
