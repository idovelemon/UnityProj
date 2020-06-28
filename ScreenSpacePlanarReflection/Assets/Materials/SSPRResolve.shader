Shader "Hidden/SSPRResolve"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
        SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            Texture2D _HashTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                uint Hash = _HashTexture[i.uv * _MainTex_TexelSize.zw].x;
                uint x = Hash & 0xFFFF;
                uint y = Hash >> 16;

                if (Hash != 0)
                {
                    float4 SrcColor = _MainTex[uint2(x, y)];
                    return SrcColor;
                }
                else
                {
                    return 0;
                }
            }
            ENDCG
        }
    }
}
