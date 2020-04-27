Shader "Custom/FlowMap"
{
    Properties
    {
        _Tex ("Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", float) = 1.0
        _CurlAmt ("Curl Weight", float) = 1.0
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
                float4 position     : POSITION;
                float2 uv           : TEXCOORD0;
            };

            sampler2D _Tex;
            float4 _Tex_ST;
            float _NoiseScale;
            float _CurlAmt;

            Varyings vs(Input input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _Tex);
                return output;
            }

            // Gradients
            static const float3 Gradients[12] = {
                float3(1, 1, 0), float3(-1, 1, 0), float3(1, -1, 0), float3(-1, -1, 0),
                float3(1, 0, 1), float3(-1, 0, 1), float3(1, 0, -1), float3(-1, 0, -1),
                float3(0, 1, 1), float3(0, -1, 1), float3(0, 1, -1), float3(0, -1, -1)
            };

            // Trilinear interpolating method
            float3 quintic(float3 x) {
                return x * x * x * (x * (x * 6.0 - 15.0) + 10.0);
            }

            // Hash Function from https://www.shadertoy.com/view/4djSRW
            float hash(float3 v) {
                float HASHSCALE = 0.1031;
                v = frac(v * HASHSCALE);
                v += dot(v, v.yzx + 19.19);
                return frac((v.x + v.y) * v.z);
            }

            // 3D Perlin noise <optimization way>
            float noise01(float x, float y, float z) {
                float3 p = floor(float3(x, y, z));
                float3 t = float3(x, y, z) - p;
                float3 f = quintic(t);

                float3 v0 = p + float3(0.0, 0.0, 0.0);
                float3 v1 = p + float3(1.0, 0.0, 0.0);
                float3 v2 = p + float3(0.0, 1.0, 0.0);
                float3 v3 = p + float3(1.0, 1.0, 0.0);
                float3 v4 = v0 + float3(0.0, 0.0, 1.0);
                float3 v5 = v1 + float3(0.0, 0.0, 1.0);
                float3 v6 = v2 + float3(0.0, 0.0, 1.0);
                float3 v7 = v3 + float3(0.0, 0.0, 1.0);

                int g0Index = int(hash(v0) * 12.0);
                int g1Index = int(hash(v1) * 12.0);
                int g2Index = int(hash(v2) * 12.0);
                int g3Index = int(hash(v3) * 12.0);
                int g4Index = int(hash(v4) * 12.0);
                int g5Index = int(hash(v5) * 12.0);
                int g6Index = int(hash(v6) * 12.0);
                int g7Index = int(hash(v7) * 12.0);
                float3 g0 = Gradients[g0Index];
                float3 g1 = Gradients[g1Index];
                float3 g2 = Gradients[g2Index];
                float3 g3 = Gradients[g3Index];
                float3 g4 = Gradients[g4Index];
                float3 g5 = Gradients[g5Index];
                float3 g6 = Gradients[g6Index];
                float3 g7 = Gradients[g7Index];

                return lerp(
                    lerp(
                        lerp(dot(g0, t - float3(0.0, 0.0, 0.0)), dot(g1, t - float3(1.0, 0.0, 0.0)), f.x),
                        lerp(dot(g2, t - float3(0.0, 1.0, 0.0)), dot(g3, t - float3(1.0, 1.0, 0.0)), f.x),
                        f.y
                    ),
                    lerp(
                        lerp(dot(g4, t - float3(0.0, 0.0, 1.0)), dot(g5, t - float3(1.0, 0.0, 1.0)), f.x),
                        lerp(dot(g6, t - float3(0.0, 1.0, 1.0)), dot(g7, t - float3(1.0, 1.0, 1.0)), f.x),
                        f.y
                    ),
                    f.z
                );
            }

            // Fratical Browian Motion
            float fBm(int octaves, float x, float y, float z) {
                float amplitude = 1.0;
                float frequency = 1.0;
                float result = 0.0;
                float totalAmplitude = 0.0;

                for (int i = 0; i < octaves; i++) {
                    float3 pos = frequency * float3(x, y, z);
                    result = result + amplitude * noise01(pos.x, pos.y, pos.z);

                    totalAmplitude = totalAmplitude + amplitude;
                    amplitude = amplitude / 2.0;
                    frequency = frequency * 2.0;
                }

                result = result / totalAmplitude;
                return result;
            }

            half4 fs(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Get noise value as flow vector
                float uvz = 3.0f;
                float eps = 0.0001f;
                float a = fBm(4, uv.x * _NoiseScale, uv.y * _NoiseScale, uvz);
                float b = fBm(4, uv.x * _NoiseScale, uv.y * _NoiseScale - eps, uvz);
                float c = fBm(4, uv.x * _NoiseScale - eps, uv.y * _NoiseScale, uvz);
                float2 curl = float2(a - b, a - c);
                curl = curl / float2(eps, eps);
                curl.y = -curl.y;
                curl = normalize(curl);

                // Offset time
                float cycle = 0.15f, halfCycle = cycle / 2.0f;

                float time0 = (_Time - floor(_Time / cycle) * cycle) / cycle;
                time0 = time0 - 0.5f;
                float color0 = tex2D(_Tex, input.uv + curl * _CurlAmt * time0);

                float time1 = (_Time + halfCycle - floor((_Time + halfCycle) / cycle) * cycle) / cycle;
                time1 = time1 - 0.5f;
                float color1 = tex2D(_Tex, input.uv + curl * _CurlAmt * time1);

                return lerp(color0, color1, abs(time0) / 0.5f);
            }

            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
