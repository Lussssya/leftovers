Shader "Leftovers/IntroCloud"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 screen:TEXCOORD1; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; o.screen=ComputeScreenPos(o.pos); return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p) {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float _CloudTime;
            float _MotionAmount;
            fixed4 frag(v2f i):SV_Target {
                float2 p=i.uv*2-1;
                float n=noise(i.uv*5 + float2(sin(_CloudTime), cos(_CloudTime)) * _MotionAmount)+noise(i.uv*11 + _CloudTime * _MotionAmount)*.35;
                float density=saturate(1-length(p)+ (n-.6)*.28);
                float alpha=smoothstep(0,.65,density)*i.color.a*.6;
                float2 screenUV=i.screen.xy/i.screen.w;
                float edge=min(min(screenUV.x,1-screenUV.x),min(screenUV.y,1-screenUV.y));
                alpha*=smoothstep(0,.08,edge);
                float light=.67+.33*saturate(.5+p.y*.4-p.x*.2);
                return float4(i.color.rgb*light,alpha);
            }
            ENDCG
        }
    }
}
