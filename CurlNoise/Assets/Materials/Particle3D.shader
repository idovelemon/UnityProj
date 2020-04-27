Shader "Custom/Particle3D"
{
    Properties
    {
        _Color("Particle Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Scale("Particle Size", float) = 1.0
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags{ "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        LOD 300

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags{ "LightMode" = "UniversalForward" }

            Blend One One
            Cull Off

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #pragma target 5.0
            #pragma vertex vs
            #pragma fragment fs
            #pragma enable_d3d11_debug_symbols

            struct Input
            {
                uint vertexID : SV_VertexID;
            };

            StructuredBuffer<float3> positionBuffer;
            float4 _Color;
            float _Scale;

            static const float4 vertices[6] = {
                float4(-1.0f, 1.0f, 0.5f, 1.0f),
                float4(1.0f, 1.0f, 0.5f, 1.0f),
                float4(1.0f, -1.0f, 0.5f, 1.0f),
                float4(-1.0f, 1.0f, 0.5f, 1.0f),
                float4(1.0f, -1.0f, 0.5f, 1.0f),
                float4(-1.0f, -1.0f, 0.5f, 1.0f)
            };

            Varyings vs(Input input)
            {
                Varyings output = (Varyings)0;
                float4 vertex = float4(positionBuffer[input.vertexID / 6], 0.0f) + vertices[input.vertexID % 6] * _Scale;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(vertex.xyz);
                output.positionCS = vertexInput.positionCS;
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
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
