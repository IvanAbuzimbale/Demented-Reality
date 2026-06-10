Shader "DR/KawaseBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Offset ("Offset", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Offset;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 t = _MainTex_TexelSize.xy * _Offset;
                fixed4 c = tex2D(_MainTex, uv + float2( t.x,  t.y));
                c += tex2D(_MainTex, uv + float2(-t.x,  t.y));
                c += tex2D(_MainTex, uv + float2( t.x, -t.y));
                c += tex2D(_MainTex, uv + float2(-t.x, -t.y));
                return c * 0.25;
            }
            ENDCG
        }
    }

    Fallback Off
}
