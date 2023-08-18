Shader "Custom/RemapRGB"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorR ("Color R", Color) = (1.0, 0.0, 0.0, 0.0)
        _ColorG ("Color G", Color) = (0.0, 1.0, 0.0, 0.0)
        _ColorB ("Color B", Color) = (0.0, 0.0, 1.0, 0.0)
        _ColorA ("Color A", Color) = (0.0, 0.0, 0.0, 1.0)
        _Color ("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _ColorR;
            float4 _ColorG;
            float4 _ColorB;
            float4 _ColorA;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return mul(col, float4x4(_ColorR, _ColorG, _ColorB, _ColorA)) * i.color;
            }
            ENDCG
        }
    }
}
