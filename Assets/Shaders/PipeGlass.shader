// Chastain Rush, Round 21 follow-ups (Dan: "can we have the pipe be clear and you can see fish swimming around?"): the clear stretch
// of pipe P1, glass seen from inside. Thin tinted water-glass: nearly clear looking straight through, more tint and a sheen at
// grazing angles (fresnel), a soft moving glint so it reads as glass. Transparent, no depth write, both faces. Kept self-contained
// (Core.hlsl only) so the exe draws it the same as the Editor.
Shader "ChastainRush/PipeGlass"
{
    Properties
    {
        _BaseColor ("Tint (alpha: straight through)", Color) = (0.55, 0.85, 0.9, 0.08)
        _EdgeColor ("Edge tint (alpha: grazing)", Color) = (0.75, 0.95, 1.0, 0.55)
        _Power ("Fresnel power", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Glass"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half4 _EdgeColor; float _Power;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 world : TEXCOORD1; };
            V vert(A a)
            {
                V v; v.world = TransformObjectToWorld(a.positionOS.xyz);
                v.positionCS = TransformWorldToHClip(v.world); v.normalWS = TransformObjectToWorldNormal(a.normalOS);
                return v;
            }
            half4 frag(V v) : SV_Target
            {
                float3 n = normalize(v.normalWS); float3 e = normalize(_WorldSpaceCameraPos - v.world);
                float f = pow(1 - saturate(abs(dot(n, e))), _Power);
                half4 c = lerp(_BaseColor, _EdgeColor, f);
                // a faint glint that drifts along the glass, only here and there
                float g = sin(v.world.x * 0.35 + v.world.z * 0.27 + v.world.y * 1.1 + _Time.y * 0.4);
                float patch = saturate(sin(v.world.x * 0.07 - v.world.z * 0.05 + _Time.y * 0.1) * 2 - 1.2);
                float glint = smoothstep(0.996, 1.0, g) * patch;
                c.rgb += glint * 0.25; c.a += glint * 0.08;
                return c;
            }
            ENDHLSL
        }
    }
}
