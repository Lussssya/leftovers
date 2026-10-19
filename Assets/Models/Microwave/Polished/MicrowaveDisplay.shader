Shader "Leftovers/Microwave Display"
{
    Properties
    {
        _MainTex ("Timer atlas", 2D) = "black" {}
        _Glitch ("Glitch", Range(0,1)) = 0
        _GlitchFrame ("Glitch frame", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Glitch;
            float _GlitchFrame;
            struct Input { float4 vertex: POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 vertex: SV_POSITION; float2 uv:TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float3 Sample(float2 uv)
            {
                return tex2D(_MainTex,clamp(uv,.005,.995)*_MainTex_ST.xy+_MainTex_ST.zw).rgb;
            }
            fixed4 frag(Output i): SV_Target
            {
                float2 uv = i.uv;
                if (_Glitch < .001) return fixed4(Sample(uv),1);
                float band = floor(uv.y*13);
                float jitter = frac(sin(band*17+_GlitchFrame)*43758.54)-.5;
                uv.x += jitter*.16*_Glitch;
                if (uv.x < .43) uv.x += .51;
                float split = .022*_Glitch;
                float r = Sample(uv+float2(split,0)).r;
                float g = Sample(uv).r;
                float b = Sample(uv-float2(split,.012)).r;
                float scan = .7+.3*step(.3,frac(i.uv.y*52+_GlitchFrame));
                return fixed4(float3(g, max(0,r-g)*.95, max(0,b-g)*.95)*scan,1);
            }
            ENDCG
        }
    }
}


