Shader "Racer/NightStars"
{
    // 0.72 night sky: soft round star points (particle billboards), additive, no fog, drawn after the sky (the skybox paints over anything queued before it).
    Properties { _Visibility("Visibility",Range(0,1))=1 }
    SubShader
    {
        Tags { "Queue"="Transparent-50" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Visibility;
            CBUFFER_END
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            // 0.119: the dome is 600 m out but the world-edge hills stand up to ~1.8 km out, so the stars were drawn over them. Each star's
            // depth is pushed to the far plane: only open sky (cleared depth) shows it; terrain, hills, trees, buildings and clouds hide it.
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.p.z=UNITY_RAW_FAR_CLIP_VALUE*v.p.w;v.uv=a.uv;v.c=a.c;return v;}
            half4 frag(V v):SV_Target {float2 d=v.uv-.5;float s=saturate(1-dot(d,d)*4);s*=s;return half4(v.c.rgb*s*_Visibility,0);}
            ENDHLSL
        }
    }
}
