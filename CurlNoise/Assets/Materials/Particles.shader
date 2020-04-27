Shader "Custom/Particles"
{
    Properties
    {
        _Color("Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _Scale("Scale", float) = 0.0015
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        LOD 300

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags{ "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vs
            #pragma fragment fs
            #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            struct Input
            {
                uint vertexID : SV_VertexID;
            };

            static const float4 vertices[6] = {
                float4(-1.0f, 1.0f, 0.5f, 1.0f),
                float4(1.0f, 1.0f, 0.5f, 1.0f),
                float4(1.0f, -1.0f, 0.5f, 1.0f),
                float4(-1.0f, 1.0f, 0.5f, 1.0f),
                float4(1.0f, -1.0f, 0.5f, 1.0f),
                float4(-1.0f, -1.0f, 0.5f, 1.0f)
            };

            StructuredBuffer<float2> positionBuffer;
            float4 _Color;
            float _Scale;

            Varyings vs(Input input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = float4(positionBuffer[input.vertexID / 6], 0.0f, 0.0f) + vertices[input.vertexID % 6] * _Scale;
                output.positionCS.w = 1.0f;
                return output;
            }

            half4 fs(Varyings input) : SV_Target
            {
                half4 result = _Color * 3.0f;
                result.w = 1.0f;
                return result;
            }

            ENDHLSL
        }
    }
}
