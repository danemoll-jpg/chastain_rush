// Chastain Rush, Round 21 follow-ups: the water round P1's clear stretch, seen from inside it. One unlit shader, two uses:
//   the surface overhead   (opaque) the pond's surface seen from below: its texture drifts two ways at once, so the ripples move
//   light shafts           (additive, _Add 1) soft beams down from the surface, fading with depth (uv.y) and flickering slowly
// Self-contained (Core.hlsl only) so the exe draws it the same as the Editor.
Shader "ChastainRush/UnderwaterScroll"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Scroll ("Scroll (xy one layer, zw the other)", Vector) = (0.03, 0.02, -0.02, 0.035)
        [Enum(UnityEngine.Rendering.BlendMode)] _Src ("Src blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _Dst ("Dst blend", Float) = 0
        _ZWrite ("ZWrite", Float) = 1
        _Shaft ("Shaft (fade along uv.y, flicker)", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Underwater"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_Src] [_Dst]
            ZWrite [_ZWrite]
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; float4 _Scroll; float _Src, _Dst, _ZWrite, _Shaft, _Cull;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float3 world : TEXCOORD1; };
            V vert(A a)
            {
                V v; v.world = TransformObjectToWorld(a.positionOS.xyz); v.positionCS = TransformWorldToHClip(v.world);
                v.uv = a.uv; v.color = a.color; return v;
            }
            half4 frag(V v) : SV_Target
            {
                float2 uv = TRANSFORM_TEX(v.uv, _BaseMap);
                if (_Shaft > 0)
                {
                    // across the beam a soft band (uv.x 0..1), down it a fade (uv.y 0 at the surface), a slow flicker per beam
                    float across = sin(v.uv.x * 3.14159); float down = 1 - v.uv.y;
                    float flick = 0.6 + 0.4 * sin(_Time.y * 0.8 + v.color.r * 37.0);
                    float a = across * across * down * down * flick;
                    return half4(_BaseColor.rgb * a * v.color.a, 1);
                }
                half4 t1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + _Time.y * _Scroll.xy);
                half4 t2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * 1.37 + _Time.y * _Scroll.zw);
                return half4(_BaseColor.rgb * (0.55 + 0.45 * (t1.rgb * t2.rgb * 1.6)) * v.color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
