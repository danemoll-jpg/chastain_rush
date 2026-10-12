// Chastain Rush, Round 21 follow-ups: the fish round P1's clear stretch (PipeAquarium draws them instanced). Colored per vertex,
// lit softly from above as under water (no shadows), and the tail wags (_Mode 1: a weed sways instead, see vert): each vertex swings sideways by how far back it sits (the
// mesh's -z is the tail), each fish at its own pace from where it is (_Wag: wags per second, set per school by the material).
// Self-contained (Core.hlsl only) so the exe draws it the same as the Editor.
Shader "ChastainRush/Fish"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Wag ("Wags per second", Float) = 2.5
        _WagSize ("Wag size (m at the tail per m of body)", Float) = 0.12
        _Mode ("0 fish, 1 weed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Fish"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; float _Wag, _WagSize, _Mode;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float4 color : COLOR; };
            V vert(A a)
            {
                UNITY_SETUP_INSTANCE_ID(a);
                if (_Mode > 0.5)
                {
                    // a weed: world space (the mesh may be batched), vertex alpha = how far up the blade (0 at the root)
                    float3 w = TransformObjectToWorld(a.positionOS.xyz); float ph = dot(w.xz, float2(0.73, 0.51)) * 2.0;
                    float k = a.color.a * a.color.a * _WagSize;
                    w.xz += float2(sin(_Time.y * _Wag * 6.2832 + ph), cos(_Time.y * _Wag * 5.1 + ph * 1.3)) * k;
                    V vw; vw.positionCS = TransformWorldToHClip(w); vw.normalWS = TransformObjectToWorldNormal(a.normalOS); vw.color = half4(a.color.rgb, 1);
                    return vw;
                }
                float3 o = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float phase = dot(o, float3(1.7, 2.3, 1.1));
                float back = saturate(-a.positionOS.z * 2.2 + 0.2);   // the mesh is 1 long, nose at +0.5
                float3 p = a.positionOS.xyz; p.x += sin(_Time.y * _Wag * 6.2832 + phase - a.positionOS.z * 4.0) * _WagSize * back;
                V v; v.positionCS = TransformObjectToHClip(p); v.normalWS = TransformObjectToWorldNormal(a.normalOS); v.color = a.color;
                return v;
            }
            half4 frag(V v) : SV_Target
            {
                float3 n = normalize(v.normalWS);
                half light = 0.45 + 0.55 * saturate(n.y * 0.6 + 0.5);
                half3 c = v.color.rgb * _BaseColor.rgb * light * half3(0.85, 1.0, 1.0);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
