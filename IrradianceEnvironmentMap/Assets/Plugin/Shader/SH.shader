Shader "Unlit/SH"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _SH_L00("L00", Vector) = (1,1,1)
        _SH_L11("L11", Vector) = (1,1,1)
        _SH_L10("L10", Vector) = (1,1,1)
        _SH_L1_1("L1_1", Vector) = (1,1,1)
        _SH_L22("L22", Vector) = (1,1,1)
        _SH_L21("L21", Vector) = (1,1,1)
        _SH_L20("L20", Vector) = (1,1,1)
        _SH_L2_1("L2_1", Vector) = (1,1,1)
        _SH_L2_2("L2_2", Vector) = (1,1,1)
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float3 _SH_L00;
            float3 _SH_L11;
            float3 _SH_L10;
            float3 _SH_L1_1;
            float3 _SH_L22;
            float3 _SH_L21;
            float3 _SH_L20;
            float3 _SH_L2_1;
            float3 _SH_L2_2;

            float3 SH_Evaluation(float3 n, float3 l00, float3 l11, float3 l10, float3 l1_1,
                float3 l22, float3 l21, float3 l20, float3 l2_1, float3 l2_2)
            {
                n = float3(n.x, n.z, n.y);
                float c1 = 0.429043f;
                float c2 = 0.511664f;
                float c3 = 0.743125f;
                float c4 = 0.886227f;
                float c5 = 0.247708f;

                return c1 * l22 * (n.x * n.x - n.y * n.y) +
                    c3 * l20 * n.z * n.z +
                    c4 * l00 - c5 * l20 +
                    2 * c1 * (l2_2 * n.x * n.y + l21 * n.x * n.z + l2_1 * n.y * n.z) +
                    2 * c2 * (l11 * n.x + l1_1 * n.y + l10 * n.z);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldNormal = mul(i.normal, (float3x3)unity_WorldToObject);
                worldNormal = normalize(worldNormal);

                float3 irradiance = SH_Evaluation(worldNormal, _SH_L00, _SH_L11, _SH_L10, _SH_L1_1,
                    _SH_L22, _SH_L21, _SH_L20, _SH_L2_1, _SH_L2_2);

                return float4(irradiance / 3.14159f, 1.0f) * _Color;
            }
            ENDCG
        }
    }
}
