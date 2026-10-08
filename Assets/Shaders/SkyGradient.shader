// Chastain Rush sky: a plain gradient from a horizon colour up to a zenith colour, with a sun glow. The horizon colour
// is set to the same colour as the distance fog (MapBuilder does this), so the far edge of the map fades into the sky
// instead of ending in a hard line.
Shader "ChastainRush/SkyGradient"
{
    Properties
    {
        _Horizon ("Horizon colour (= fog)", Color) = (0.74, 0.80, 0.87, 1)
        _Zenith ("Zenith colour", Color) = (0.28, 0.48, 0.80, 1)
        _SunColor ("Sun glow colour", Color) = (1.0, 0.92, 0.75, 1)
        _SunDir ("Direction to the sun", Vector) = (0.3, 0.7, 0.6, 0)
        _Haze ("Haze height (0..1)", Float) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Horizon;
                half4 _Zenith;
                half4 _SunColor;
                float4 _SunDir;
                float _Haze;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A a)
            {
                V v;
                v.positionCS = TransformObjectToHClip(a.positionOS.xyz);
                v.dir = a.positionOS.xyz;
                return v;
            }
            half4 frag(V v) : SV_Target
            {
                float3 d = normalize(v.dir);
                float t = saturate(d.y / max(_Haze, 0.05));
                half3 c = lerp(_Horizon.rgb, _Zenith.rgb, smoothstep(0.0, 1.0, pow(t, 0.7)));
                float s = saturate(dot(d, normalize(_SunDir.xyz)));
                c += _SunColor.rgb * (pow(s, 600.0) * 4.0 + pow(s, 14.0) * 0.25);
                if (d.y < 0) c = _Horizon.rgb; // below the horizon: the haze colour, so the map's edge blends into it
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
