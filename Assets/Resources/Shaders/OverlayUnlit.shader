// Round 79 (C): a flat color drawn over everything (no depth test): the air race's 3D arrow (AirGuideArrow), so the plane's own nose and cockpit
// never hide it in the first-person view. URP's Unlit shader has no depth-test setting.
Shader "ChastainRush/OverlayUnlit"
{
    Properties { _BaseColor ("Color", Color) = (0.35, 1, 0.25, 1) }
    SubShader
    {
        Tags { "RenderType" = "Overlay" "Queue" = "Overlay" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Back
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 pos : SV_POSITION; half shade : TEXCOORD0; };
            V vert(A i)
            {
                V o; o.pos = TransformObjectToHClip(i.pos.xyz);
                float3 n = TransformObjectToWorldNormal(i.n);
                o.shade = (half)(0.78 + 0.22 * saturate(n.y));   // its top a little lighter than its sides: it reads as a solid arrow
                return o;
            }
            half4 frag(V i) : SV_Target { return half4(_BaseColor.rgb * i.shade, 1); }
            ENDHLSL
        }
    }
}
