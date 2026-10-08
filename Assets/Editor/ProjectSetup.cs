using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ChastainRush
{
    // The Racer car's materials use URP Lit. Creates a plain URP asset for this project (instead of copying Racer's
    // render settings) and makes it active.
    static class ProjectSetup
    {
        const string RpAssetPath = "Assets/Settings/URP.asset";
        const string RendererPath = "Assets/Settings/URP_Renderer.asset";

        public static void EnsureUrp()
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RpAssetPath);
            if (rp == null)
            {
                System.IO.Directory.CreateDirectory("Assets/Settings");
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                rp = UniversalRenderPipelineAsset.Create(renderer);
                rp.shadowDistance = 150f;
                AssetDatabase.CreateAsset(rp, RpAssetPath);
                AssetDatabase.SaveAssets();
            }
            if (GraphicsSettings.defaultRenderPipeline != rp) GraphicsSettings.defaultRenderPipeline = rp;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != null) QualitySettings.renderPipeline = null;
            }
            AssetDatabase.SaveAssets();
        }
    }
}
