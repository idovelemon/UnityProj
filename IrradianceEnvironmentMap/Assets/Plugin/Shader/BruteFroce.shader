Shader "Unlit/BruteFroce"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _MainTex ("IrradianceMap", Cube) = "white" {}
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
                float3 normal : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            samplerCUBE _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.normal = v.normal;
                o.vertex = UnityObjectToClipPos(v.vertex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldNormal = mul(i.normal, (float3x3)unity_WorldToObject);
                worldNormal = normalize(worldNormal);

                float3 radiance = texCUBE(_MainTex, worldNormal).xyz;
                radiance = _Color.xyz * radiance;
                return float4(radiance, 1.0f);
            }
            ENDCG
        }
    }
}
