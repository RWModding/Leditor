Shader "Unlit/DepthSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DepthTex ("Depth", 2D) = "white" {}
        _DepthOffset ("Depth Offset", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct fOut
            {
                fixed4 color : SV_Target;
                float depth : SV_Depth;
            };

            sampler2D _MainTex;
            sampler2D _DepthTex;
            float4 _MainTex_ST;
            float _DepthOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);// +mul(UNITY_MATRIX_P, float4(0.0, 0.0, _DepthOffset, 0.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;

                // o.vertex = mul(P, float3(0,0,offset,1)) + v.vertex;
                return o;
            }

            fOut frag(v2f i)
            {
                fOut o;
                o.color = tex2D(_MainTex, i.uv) * i.color;

                float3 h = mul((float3x3)UNITY_MATRIX_VP, float3(0.0, 0.0, _DepthOffset * tex2D(_DepthTex, i.uv).r));
                o.depth = i.vertex.z + h.z;
                //o.depth = i.vertex.z;
                return o;
            }
            ENDCG
        }
    }
}
