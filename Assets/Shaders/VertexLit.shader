// Chastain Rush: one simple lit shader for everything that is coloured per vertex: house walls and roofs, road
// markings, and the Racer tree kit. Based on Racer's "Racer/Foliage" shader (wind sway driven by vertex alpha,
// per-tree leaf tint from the instance position, shadows) with Racer's own lighting include removed: this is plain
// URP lighting (main light + shadows + ambient SH + fog).
//   _Sway      0 for static meshes; 1 for trees (vertex alpha = how much a vertex moves, uv0.x = 1 on leaves)
//   _LeafTint  1 for trees: patch-coherent darker/lighter green per tree, from where the tree stands
//   _OffsetFactor/_OffsetUnits  depth bias, so paint lies on a road without flickering
Shader "ChastainRush/VertexLit"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Sway ("Wind sway", Float) = 0
        _LeafTint ("Leaf tint", Float) = 0
        _OffsetFactor ("Depth offset factor", Float) = 0
        _OffsetUnits ("Depth offset units", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Sway;
            float _LeafTint;
            float _OffsetFactor;
            float _OffsetUnits;
        CBUFFER_END
        struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        float3 Origin() { return float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23); }
        float3 Sway(float3 world, float weight)
        {
            if (_Sway <= 0) return world;
            float3 o = Origin();
            float height = length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21));
            float t = _Time.y;
            float phase = dot(o.xz, float2(0.071, 0.053));
            float gust = 0.6 + 0.4 * sin(t * 0.37 + phase * 0.5);
            float strength = 0.010 * height * gust * weight;
            return world + float3(sin(t * 1.7 + phase + world.y * 0.25), 0, cos(t * 1.3 + phase * 1.4 + world.x * 0.2)) * strength;
        }
        float Noise2(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            float a = frac(sin(dot(i, float2(12.9898, 78.233))) * 43758.5453);
            float b = frac(sin(dot(i + float2(1, 0), float2(12.9898, 78.233))) * 43758.5453);
            float c = frac(sin(dot(i + float2(0, 1), float2(12.9898, 78.233))) * 43758.5453);
            float d = frac(sin(dot(i + float2(1, 1), float2(12.9898, 78.233))) * 43758.5453);
            f = f * f * (3 - 2 * f);
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Offset [_OffsetFactor], [_OffsetUnits]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct V
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float4 color : COLOR;
                float3 world : TEXCOORD1;
                float fog : TEXCOORD2;
                float leaf : TEXCOORD3;
                float3 origin : TEXCOORD4;
            };

            V vert(A a)
            {
                UNITY_SETUP_INSTANCE_ID(a);
                V v;
                float3 w = Sway(TransformObjectToWorld(a.positionOS.xyz), a.color.a);
                v.positionCS = TransformWorldToHClip(w);
                v.normalWS = TransformObjectToWorldNormal(a.normalOS);
                v.color = a.color;
                v.world = w;
                v.fog = ComputeFogFactor(v.positionCS.z);
                v.leaf = a.uv.x;
                v.origin = Origin();
                return v;
            }

            half4 frag(V v) : SV_Target
            {
                half3 albedo = v.color.rgb * _BaseColor.rgb;
                if (_LeafTint > 0)
                {
                    // broad patches of darker and lighter green, a little per tree
                    float patch = Noise2((v.origin.xz + float2(913, 771)) / 65);
                    float hash = frac(sin(dot(v.origin.xz, float2(12.9898, 78.233))) * 43758.5453);
                    half3 tint = lerp(half3(0.80, 0.92, 0.78), half3(1.12, 1.06, 0.92), saturate(patch * 0.8 + hash * 0.2));
                    albedo *= lerp(half3(1, 1, 1), tint, v.leaf);
                }
                float3 n = normalize(v.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(v.world));
                half3 direct = light.color * saturate(dot(n, light.direction)) * light.shadowAttenuation;
                half3 c = albedo * (direct + SampleSH(n));
                c = MixFog(c, v.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;

            float4 vert(A a) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(a);
                float3 w = Sway(TransformObjectToWorld(a.positionOS.xyz), a.color.a);
                float3 n = TransformObjectToWorldNormal(a.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 l = normalize(_LightPosition - w);
                #else
                float3 l = _LightDirection;
                #endif
                float4 p = TransformWorldToHClip(ApplyShadowBias(w, n, l));
                #if UNITY_REVERSED_Z
                p.z = min(p.z, UNITY_NEAR_CLIP_VALUE);
                #else
                p.z = max(p.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return p;
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            float4 vert(A a) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(a);
                return TransformWorldToHClip(Sway(TransformObjectToWorld(a.positionOS.xyz), a.color.a));
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
