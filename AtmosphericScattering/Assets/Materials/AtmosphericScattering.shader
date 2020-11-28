Shader "Custom/AtmosphericScattering"
{
    Properties
    {
        _SunDirection("Sun Direction", Vector) = (1.0, 1.0, 1.0, 1.0)
        [HDR]_SunIntensity("Sun Intensity", Color) = (40.0, 40.0, 40.0, 1.0)
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex ASVert
            #pragma fragment ASFrag
            #pragma enable_d3d11_debug_symbols
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #include "AtmosphericScattering.hlsl"

            struct ASInput
            {
                float4 position : POSITION;
                float4 normal   : NORMAL;
                float4 tangent  : TANGENT;
                float2 uv       : TEXCOORD0;
            };

            struct ASOutput
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            float3 _SunDirection;
            float3 _CameraPos;
            float3 _SunIntensity;

            ASOutput ASVert(ASInput input)
            {
                ASOutput output = (ASOutput)0;

                output.positionCS = input.position * 2.0f;
                output.positionCS.z = 1.0f;
                output.positionCS.w = 1.0f;
                output.uv = input.uv;

                return output;
            }

            float3 CalculateCameraVector(float2 coord, float2 screen)
            {
                coord.y = 1.0f - coord.y;
                float2 uv = 2.0f * (coord.xy - float2(0.5f, 0.5f));
                return normalize(float3(uv.x, uv.y, -1.0f));
            }

            float4 ASFrag(ASOutput output) : SV_TARGET
            {
                float3 planetPos = float3(0.0f, 0.0f, 0.0f);
                float planetRadius = 6371e3;
                float atmosphereRadius = 6471e3;
                uint uViewRaySampleN = 64u;
                uint uLightRaySampleN = 4u;
                float3 sunIntensity = _SunIntensity;
                float3 sunDir = normalize(_SunDirection);

                float3 InSky = float3(0.0f, atmosphereRadius, atmosphereRadius);
                float3 InGround = float3(0.0f, planetRadius + 100.0f, 0.0f);
                float3 cameraPos = InGround;

                float3 cameraView = CalculateCameraVector(output.uv, _ScreenParams.xy);
                float3 rayleighColor = RayleighAtmosphericScatteringIntegration(
                    cameraPos, cameraView,
                    planetPos, atmosphereRadius, planetRadius,
                    uViewRaySampleN, uLightRaySampleN,
                    sunIntensity, sunDir
                );
                float3 mieColor = MieAtmosphericScatteringIntegration(
                    cameraPos, cameraView,
                    planetPos, atmosphereRadius, planetRadius,
                    uViewRaySampleN, uLightRaySampleN,
                    sunIntensity, sunDir
                );

                float3 color = rayleighColor + mieColor;
                return float4(color * 1.0f, 1.0f);
            }

            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
