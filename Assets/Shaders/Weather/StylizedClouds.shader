Shader "Racer/StylizedClouds"
{
    // 0.73 sky clouds: faceted low-poly puffs lit by the main light (sun or moon) with a soft wrap and the sky ambient, a
    // flash term for lightning inside the cloud, and their own gentle haze towards the horizon colour (the world's linear
    // fog would erase them at sky distances). Opaque, queued just after the stars (Transparent-50) so a cloud hides the stars
    // behind it, and before rain/snow so falling weather still shows against it. No shadows.
    Properties
    {
        _Color("Cloud colour",Color)=(1,1,1,1)
        _Shade("Underside darkening",Range(0,1))=.35
        _Flash("Lightning",Float)=0
        _Haze("Haze colour",Color)=(.7,.77,.86,1)
        _HazeAmount("Haze amount",Range(0,1))=.45
        _HazeDistance("Haze distance",Float)=1700
    }
    SubShader
    {
        Tags { "Queue"="Transparent-40" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color; half _Shade; half _Flash; half4 _Haze; half _HazeAmount; float _HazeDistance;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float4 c:COLOR; };
            struct V { float4 p:SV_POSITION; float3 n:TEXCOORD0; float3 w:TEXCOORD1; float h:TEXCOORD2; };
            V vert(A a)
            {
                V v; v.w=TransformObjectToWorld(a.p.xyz); v.p=TransformWorldToHClip(v.w); v.n=TransformObjectToWorldNormal(a.n);
                v.h=a.c.r; // 0 = cloud base, 1 = top (baked per vertex)
                return v;
            }
            half4 frag(V v):SV_Target
            {
                float3 n=normalize(v.n);
                Light sun=GetMainLight();
                half wrap=saturate(dot(n,sun.direction)*.6+.4);
                half3 ambient=SampleSH(n);
                half3 lit=_Color.rgb*(ambient*1.05+sun.color*wrap*.85);
                lit*=lerp(1-_Shade,1,saturate(v.h));
                lit+=_Flash*half3(.85,.88,1)*(.55+.45*saturate(v.h));
                float d=distance(v.w,GetCameraPositionWS());
                lit=lerp(lit,_Haze.rgb,_HazeAmount*saturate(d/_HazeDistance));
                return half4(lit,1);
            }
            ENDHLSL
        }
    }
}
