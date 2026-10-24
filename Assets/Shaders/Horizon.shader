// Round 66 (the map's edges, World/EdgeScenery.cs): the scenery past the map's edge and the horizon.
//   _Mode 0, the ring (ground and building blocks past the edge, vertex coloured): lit like ChastainRush/VertexLit (main light, ambient,
//            the scene's fog, snow and wet on upward faces), cut off past _HorizonNear (the opened view's fog end) where the horizon takes over.
//   _Mode 1, the horizon (the far land and the towers): drawn only past _HorizonNear, where the scene's fog is solid: the plain is the fog's
//            own colour; what stands up out of it (uv1.x 0: the land's prominence in vertex alpha; uv1.x 1: a tower) shows through the haze as
//            a silhouette, fainter the farther it is (_HorizonVis: the air's visibility in metres, small in rain, snow and mist). At night the
//            towers' windows light (uv0: metres round and up the tower; uv1.y its height) and the tallest carry red lights on top.
// Globals (EdgeScenery sets them every frame): _HorizonNear, _HorizonVis, _HorizonNight; Racer's WorldLook: _RacerWet, _RacerSnow.
Shader "ChastainRush/Horizon"
{
    Properties
    {
        _Mode ("0 ring, 1 horizon", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Mode;
            CBUFFER_END
            float _HorizonNear, _HorizonVis, _HorizonNight, _RacerWet, _RacerSnow;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float2 uv : TEXCOORD0; float2 uv1 : TEXCOORD1; };
            struct V
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float4 color : COLOR;
                float3 world : TEXCOORD1;
                float fog : TEXCOORD2;
                float depth : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float2 uv1 : TEXCOORD5;
            };
            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }
            float Noise2(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            V vert(A a)
            {
                V v;
                float3 w = TransformObjectToWorld(a.positionOS.xyz);
                v.positionCS = TransformWorldToHClip(w);
                v.normalWS = TransformObjectToWorldNormal(a.normalOS);
                v.color = a.color; v.world = w; v.uv = a.uv; v.uv1 = a.uv1;
                v.fog = ComputeFogFactor(v.positionCS.z);
                v.depth = -TransformWorldToView(w).z;
                return v;
            }

            half4 frag(V v) : SV_Target
            {
                float3 n = normalize(v.normalWS);
                Light light = GetMainLight();
                if (_Mode < 0.5)
                {
                    clip(_HorizonNear - v.depth);
                    half3 albedo = v.color.rgb * (0.86 + 0.28 * Noise2(v.world.xz / 90.0)) ;
                    half up = saturate((n.y - 0.55) / 0.3);
                    albedo = lerp(albedo, half3(0.88, 0.90, 0.94), _RacerSnow * up * v.color.a);
                    albedo *= 1 - 0.3 * _RacerWet * up * (1 - _RacerSnow);
                    half3 c = albedo * (light.color * saturate(dot(n, light.direction)) + SampleSH(n));
                    return half4(MixFog(c, v.fog), 1);
                }
                clip(v.depth - _HorizonNear);
                float d = length(v.world.xz - _WorldSpaceCameraPos.xz);
                half vis = exp(-d / max(1.0, _HorizonVis));
                half3 fogc = unity_FogColor.rgb;
                half lit = 0.82 + 0.18 * saturate(dot(n, light.direction));
                half3 c;
                if (v.uv1.x < 0.5)
                {
                    // the land: its prominence over the plain shows as a blue-grey silhouette
                    half3 sil = fogc * half3(0.66, 0.73, 0.84) * lit;
                    c = lerp(fogc, sil, vis * v.color.a * 0.9);
                }
                else
                {
                    // a tower: glass and stone in the day's light; at night dark with lit windows
                    half3 body = v.color.rgb * (light.color * saturate(dot(n, light.direction)) * 0.7 + SampleSH(n));
                    body = lerp(body, fogc * 0.35, _HorizonNight * 0.85);
                    c = lerp(fogc, body, vis);
                    if (_HorizonNight > 0.01)
                    {
                        float2 cell = floor(float2(v.uv.x / 3.2, v.uv.y / 3.6));
                        float2 inCell = frac(float2(v.uv.x / 3.2, v.uv.y / 3.6));
                        half on = step(0.42, Hash(cell + v.uv1.y)) * step(0.18, inCell.x) * step(inCell.x, 0.82) * step(0.25, inCell.y) * step(inCell.y, 0.75);
                        half lightsVis = exp(-d / max(1.0, _HorizonVis * 1.7));
                        c += half3(1.0, 0.82, 0.55) * on * 0.9 * _HorizonNight * lightsVis * step(abs(n.y), 0.5);
                        // the red lights on top of the tallest (blinking)
                        half top = step(v.uv1.y - 4.0, v.uv.y) * step(150.0, v.uv1.y);
                        c += half3(1.0, 0.08, 0.05) * top * step(0.5, frac(_Time.y * 0.5 + v.uv1.y * 0.013)) * 2.0 * _HorizonNight * lightsVis;
                    }
                }
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
