Shader "Racer/MoonPhase"
{
    // 0.74 the moon: a disc lit to the day's phase (_Phase 0 new, 0.5 full; waxing lit on the right, waning on the left),
    // a faint earthshine on the dark part, soft rim. Additive and fog-free, drawn after the stars and before the clouds
    // (a cloud hides it). 0.105: at the far plane, depth-tested, so everything in the world is in front of it.
    Properties { _Phase("Phase",Range(0,1))=.5 _Visibility("Visibility",Range(0,1))=1 _Color("Colour",Color)=(1,.97,.9,1) }
    SubShader
    {
        Tags { "Queue"="Transparent-45" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
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
            float _Phase; float _Visibility; half4 _Color;
            CBUFFER_END
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            // 0.105: drawn as part of the sky: the disc keeps its place and size on screen (the quad stays 900 m out) but its depth is the far
            // plane's, so terrain, mountains, trees and buildings at any distance hide it (it sets behind a ridge, never over it)
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);
            #if UNITY_REVERSED_Z
                v.p.z=v.p.w*1e-6;
            #else
                v.p.z=v.p.w*(1-1e-6);
            #endif
                v.uv=a.uv*2-1;return v;}
            half4 frag(V v):SV_Target
            {
                float r2=dot(v.uv,v.uv); if(r2>1) return 0;
                float edge=saturate((1-sqrt(r2))*14);
                float half_w=sqrt(saturate(1-v.uv.y*v.uv.y));
                float k=cos(_Phase*6.2831853);
                float x=v.uv.x;
                // signed distance (in disc widths) to the terminator; > 0 is lit
                float lit=_Phase<.5 ? (x-k*half_w) : (-k*half_w-x);
                float l=saturate(lit*9+.5);
                float shade=.82+.18*saturate(1-r2);
                float spots=.9+.1*sin(v.uv.x*7+1.3)*sin(v.uv.y*6-.4);
                half3 c=_Color.rgb*shade*spots*l+_Color.rgb*.035*(1-l);
                return half4(c*edge*_Visibility,0);
            }
            ENDHLSL
        }
    }
}
