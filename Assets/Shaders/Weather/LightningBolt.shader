Shader "Racer/LightningBolt"
{
    // 0.74 lightning: the visible forked bolt (line strips across the sky). Additive, no fog (bolts are hundreds of metres
    // away, beyond the rain haze), soft across the strip, after the clouds so a bolt shows below the cloud base.
    Properties { _Intensity("Intensity",Float)=1 }
    SubShader
    {
        Tags { "Queue"="Transparent-30" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
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
            float _Intensity;
            CBUFFER_END
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.c=a.c;return v;}
            half4 frag(V v):SV_Target {float across=1-abs(v.uv.y*2-1);float core=pow(saturate(across),1.5);return half4(v.c.rgb*v.c.a*core*_Intensity,0);}
            ENDHLSL
        }
    }
}
