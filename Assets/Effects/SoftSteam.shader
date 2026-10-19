Shader "Leftovers/Soft Steam"
{
    Properties { _Color ("Tint", Color) = (0.9,0.92,0.94,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D_float _CameraDepthTexture;
            float4 _Color;
            struct Input { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 screen:TEXCOORD1; };
            Output vert(Input v)
            {
                Output o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.screen=ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.screen.z);
                o.color=v.color*_Color;
                o.uv=v.uv;
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float fog(float2 p)
            {
                return noise(p)*.55+noise(p*2.03)*.27+noise(p*4.07)*.13+noise(p*8.1)*.05;
            }
            fixed4 frag(Output i):SV_Target
            {
                float2 uv=i.uv;
                float2 p=uv*4-float2(.07,.18)*_Time.y;
                p+=float2(fog(p+7),fog(p+19))*1.8;
                float density=smoothstep(.24,.72,fog(p));
                float2 edge=(uv-.5)*2;
                float feather=pow(saturate(1-dot(edge,edge)),1.6);
                float depth=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)));
                float soft=saturate((depth-i.screen.z)*7);
                return fixed4(i.color.rgb,i.color.a*density*feather*soft);
            }
            ENDCG
        }
    }
}
